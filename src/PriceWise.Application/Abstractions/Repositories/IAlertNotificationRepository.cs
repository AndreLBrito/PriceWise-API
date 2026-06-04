using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IAlertNotificationRepository : IRepository<AlertNotification>
{
    Task<IReadOnlyCollection<AlertNotification>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AlertNotification?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid priceAlertId,
        Guid priceHistoryId,
        CancellationToken cancellationToken = default);
}
