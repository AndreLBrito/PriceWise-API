using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyCollection<Product>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByProductUrlAsync(
        Guid userId,
        string productUrl,
        CancellationToken cancellationToken = default);
}
