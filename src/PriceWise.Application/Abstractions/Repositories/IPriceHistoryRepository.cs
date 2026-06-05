using PriceWise.Domain.Entities;
using PriceWise.Application.Common;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IPriceHistoryRepository : IRepository<PriceHistory>
{
    Task<IReadOnlyCollection<PriceHistory>> ListByProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    async Task<PagedResponse<PriceHistory>> ListByProductAsync(
        Guid userId,
        Guid productId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = await ListByProductAsync(userId, productId, cancellationToken);

        return PagedResponse<PriceHistory>.Create(
            items.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            items.Count);
    }

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
