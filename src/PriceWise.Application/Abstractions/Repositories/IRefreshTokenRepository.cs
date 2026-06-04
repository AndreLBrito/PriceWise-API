using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task RevokeActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
