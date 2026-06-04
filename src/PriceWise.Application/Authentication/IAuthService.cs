using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Authentication.Dtos;
using PriceWise.Application.Common;

namespace PriceWise.Application.Authentication;

public interface IAuthService : IService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<Result<CurrentUserResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result> RevokeRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
