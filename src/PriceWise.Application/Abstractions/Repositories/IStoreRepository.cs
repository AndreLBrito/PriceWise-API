using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IStoreRepository : IRepository<Store>
{
    Task<IReadOnlyCollection<Store>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Store?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Store?> GetByBaseUrlAsync(
        Guid userId,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
