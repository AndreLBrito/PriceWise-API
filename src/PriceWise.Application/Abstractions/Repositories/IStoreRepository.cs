using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IStoreRepository : IRepository<Store>
{
    Task<IReadOnlyCollection<Store>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<PagedResponse<Store>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = await ListByUserIdAsync(userId, cancellationToken);

        return PagedResponse<Store>.Create(
            items.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            items.Count);
    }

    Task<Store?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Store?> GetByBaseUrlAsync(
        Guid userId,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
