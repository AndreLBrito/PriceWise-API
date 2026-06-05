using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Auth;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Authentication.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly IAccessTokenProvider accessTokenProvider;
    private readonly IPasswordHasher passwordHasher;
    private readonly IRefreshTokenProvider refreshTokenProvider;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IUserRepository userRepository;
    private readonly IApplicationTelemetry telemetry;
    private readonly AuthenticationSecurityOptions securityOptions;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenProvider accessTokenProvider,
        IRefreshTokenProvider refreshTokenProvider,
        IApplicationTelemetry telemetry,
        IOptions<AuthenticationSecurityOptions> securityOptions)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.passwordHasher = passwordHasher;
        this.accessTokenProvider = accessTokenProvider;
        this.refreshTokenProvider = refreshTokenProvider;
        this.telemetry = telemetry;
        this.securityOptions = securityOptions.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.Register");
        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            telemetry.RecordError(AuthErrors.EmailAlreadyRegistered.Code);
            return Result<AuthResponse>.Failure(AuthErrors.EmailAlreadyRegistered);
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Name.Trim(), normalizedEmail, passwordHash);

        await userRepository.AddAsync(user, cancellationToken);

        var response = await CreateAuthResponseAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.Login");
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        if (user is null)
        {
            telemetry.RecordError(AuthErrors.InvalidCredentials.Code);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            telemetry.RecordError(AuthErrors.UserInactive.Code);
            return Result<AuthResponse>.Failure(AuthErrors.UserInactive);
        }

        if (user.IsLocked(DateTime.UtcNow))
        {
            telemetry.RecordError(AuthErrors.UserLocked.Code);
            return Result<AuthResponse>.Failure(AuthErrors.UserLocked);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(
                securityOptions.MaxFailedLoginAttempts,
                securityOptions.LockoutMinutes);
            await userRepository.UpdateAsync(user, cancellationToken);

            telemetry.RecordError(AuthErrors.InvalidCredentials.Code);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (user.FailedLoginAttempts > 0 || user.LockedUntilUtc is not null)
        {
            user.ResetFailedLogins();
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        var response = await CreateAuthResponseAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.RefreshToken");
        var tokenHash = refreshTokenProvider.Hash(request.RefreshToken);
        var currentRefreshToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (currentRefreshToken is null || !currentRefreshToken.IsActive)
        {
            telemetry.RecordError(AuthErrors.InvalidRefreshToken.Code);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await userRepository.GetByIdAsync(currentRefreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            telemetry.RecordError(AuthErrors.InvalidRefreshToken.Code);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        currentRefreshToken.Revoke();
        await refreshTokenRepository.UpdateAsync(currentRefreshToken, cancellationToken);

        var response = await CreateAuthResponseAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.Logout");
        var tokenHash = refreshTokenProvider.Hash(request.RefreshToken);
        var refreshToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return Result.Success();
        }

        refreshToken.Revoke();
        await refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CurrentUserResponse>> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.GetMe");
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            telemetry.RecordError(AuthErrors.UserNotFound.Code);
            return Result<CurrentUserResponse>.Failure(AuthErrors.UserNotFound);
        }

        return Result<CurrentUserResponse>.Success(new CurrentUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            user.CreatedAtUtc));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.ChangePassword");
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            telemetry.RecordError(AuthErrors.UserNotFound.Code);
            return Result.Failure(AuthErrors.UserNotFound);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            telemetry.RecordError(AuthErrors.InvalidCurrentPassword.Code);
            return Result.Failure(AuthErrors.InvalidCurrentPassword);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await userRepository.UpdateAsync(user, cancellationToken);
        await refreshTokenRepository.RevokeActiveByUserIdAsync(user.Id, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RevokeRefreshTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AuthService.RevokeRefreshTokens");
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            telemetry.RecordError(AuthErrors.UserNotFound.Code);
            return Result.Failure(AuthErrors.UserNotFound);
        }

        await refreshTokenRepository.RevokeActiveByUserIdAsync(userId, cancellationToken);

        return Result.Success();
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var accessToken = accessTokenProvider.Generate(user);
        var refreshToken = refreshTokenProvider.Generate();
        var refreshTokenHash = refreshTokenProvider.Hash(refreshToken);
        var refreshTokenEntity = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            refreshTokenProvider.GetExpirationUtc());

        await refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            accessToken.Value,
            refreshToken,
            accessToken.ExpiresAtUtc);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
