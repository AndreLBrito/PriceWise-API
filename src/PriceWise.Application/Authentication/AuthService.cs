using PriceWise.Application.Abstractions.Auth;
using PriceWise.Application.Abstractions.Repositories;
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

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenProvider accessTokenProvider,
        IRefreshTokenProvider refreshTokenProvider)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.passwordHasher = passwordHasher;
        this.accessTokenProvider = accessTokenProvider;
        this.refreshTokenProvider = refreshTokenProvider;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
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
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        var response = await CreateAuthResponseAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = refreshTokenProvider.Hash(request.RefreshToken);
        var currentRefreshToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (currentRefreshToken is null || !currentRefreshToken.IsActive)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await userRepository.GetByIdAsync(currentRefreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
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
