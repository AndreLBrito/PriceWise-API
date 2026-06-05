using PriceWise.Domain.Entities;
using PriceWise.Application.Common;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IPriceAlertRepository : IRepository<PriceAlert>
{
    Task<IReadOnlyCollection<PriceAlert>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<PagedResponse<PriceAlert>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = await ListByUserIdAsync(userId, cancellationToken);

        return PagedResponse<PriceAlert>.Create(
            items.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            items.Count);
    }

    Task<PriceAlert?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PriceAlert?> GetActiveByProductIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PriceAlert>> ListActiveByProductIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);
}
