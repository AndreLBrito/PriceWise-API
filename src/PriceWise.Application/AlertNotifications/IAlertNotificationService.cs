using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.AlertNotifications.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.AlertNotifications;

public interface IAlertNotificationService : IService
{
    Task CheckPriceAlertsAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<AlertNotificationResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<AlertNotificationResponse>> GetByIdAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
