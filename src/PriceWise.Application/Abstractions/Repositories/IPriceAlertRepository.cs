using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IPriceAlertRepository : IRepository<PriceAlert>
{
    Task<IReadOnlyCollection<PriceAlert>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

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
