using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IAlertNotificationRepository : IRepository<AlertNotification>
{
    Task<IReadOnlyCollection<AlertNotification>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<PagedResponse<AlertNotification>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = await ListByUserIdAsync(userId, cancellationToken);

        return PagedResponse<AlertNotification>.Create(
            items.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            items.Count);
    }

    Task<AlertNotification?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid priceAlertId,
        Guid priceHistoryId,
        CancellationToken cancellationToken = default);
}
