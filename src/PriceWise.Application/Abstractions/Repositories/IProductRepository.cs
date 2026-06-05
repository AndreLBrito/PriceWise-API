using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyCollection<Product>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<PagedResponse<Product>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = await ListByUserIdAsync(userId, cancellationToken);

        return PagedResponse<Product>.Create(
            items.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            items.Count);
    }

    Task<Product?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByProductUrlAsync(
        Guid userId,
        string productUrl,
        CancellationToken cancellationToken = default);
}
