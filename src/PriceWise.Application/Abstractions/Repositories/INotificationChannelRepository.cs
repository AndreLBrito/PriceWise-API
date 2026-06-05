using PriceWise.Application.Common;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.Abstractions.Repositories;

public interface INotificationChannelRepository : IRepository<NotificationChannel>
{
    Task<IReadOnlyCollection<NotificationChannel>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<PagedResponse<NotificationChannel>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = await ListByUserIdAsync(userId, cancellationToken);

        return PagedResponse<NotificationChannel>.Create(
            items.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            items.Count);
    }

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
