using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IPriceHistoryRepository : IRepository<PriceHistory>
{
    Task<IReadOnlyCollection<PriceHistory>> ListByProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<PriceHistory?> GetLatestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<PriceHistory?> GetLowestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<(decimal AveragePrice, int EntriesCount)?> GetAverageAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);
}
