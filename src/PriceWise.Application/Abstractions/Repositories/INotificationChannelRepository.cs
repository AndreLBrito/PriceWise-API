using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.Abstractions.Repositories;

public interface INotificationChannelRepository : IRepository<NotificationChannel>
{
    Task<IReadOnlyCollection<NotificationChannel>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NotificationChannel>> ListActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationChannel?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationChannel?> GetActiveByTypeAndDestinationAsync(
        Guid userId,
        NotificationChannelType type,
        string destination,
        CancellationToken cancellationToken = default);
}
