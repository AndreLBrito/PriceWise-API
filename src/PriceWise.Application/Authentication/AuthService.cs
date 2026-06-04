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

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenProvider accessTokenProvider,
        IRefreshTokenProvider refreshTokenProvider,
        IApplicationTelemetry telemetry)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.passwordHasher = passwordHasher;
        this.accessTokenProvider = accessTokenProvider;
        this.refreshTokenProvider = refreshTokenProvider;
        this.telemetry = telemetry;
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

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            telemetry.RecordError(AuthErrors.InvalidCredentials.Code);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
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
            accessToken.Value,
            refreshToken,
            accessToken.ExpiresAtUtc);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
