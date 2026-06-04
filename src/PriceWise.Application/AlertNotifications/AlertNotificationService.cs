using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.AlertNotifications.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.AlertNotifications;

public sealed class AlertNotificationService : IAlertNotificationService
{
    private readonly IAlertNotificationRepository alertNotificationRepository;
    private readonly IPriceAlertRepository priceAlertRepository;

    public AlertNotificationService(
        IAlertNotificationRepository alertNotificationRepository,
        IPriceAlertRepository priceAlertRepository)
    {
        this.alertNotificationRepository = alertNotificationRepository;
        this.priceAlertRepository = priceAlertRepository;
    }

    public async Task CheckPriceAlertsAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default)
    {
        var priceAlerts = await priceAlertRepository.ListActiveByProductIdAsync(
            priceHistory.UserId,
            priceHistory.ProductId,
            cancellationToken);

        foreach (var priceAlert in priceAlerts.Where(priceAlert => priceHistory.Price <= priceAlert.TargetPrice))
        {
            var exists = await alertNotificationRepository.ExistsAsync(
                priceAlert.Id,
                priceHistory.Id,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            var notification = AlertNotification.Create(
                priceHistory.UserId,
                priceAlert.Id,
                priceHistory.ProductId,
                priceHistory.Id,
                priceHistory.Price,
                priceAlert.TargetPrice);

            await alertNotificationRepository.AddAsync(notification, cancellationToken);
        }
    }

    public async Task<Result<IReadOnlyCollection<AlertNotificationResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await alertNotificationRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = notifications.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<AlertNotificationResponse>>.Success(response);
    }

    public async Task<Result<AlertNotificationResponse>> GetByIdAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await alertNotificationRepository.GetByIdAsync(
            notificationId,
            userId,
            cancellationToken);

        return notification is null
            ? Result<AlertNotificationResponse>.Failure(AlertNotificationErrors.AlertNotificationNotFound)
            : Result<AlertNotificationResponse>.Success(MapToResponse(notification));
    }

    private static AlertNotificationResponse MapToResponse(AlertNotification notification)
    {
        return new AlertNotificationResponse(
            notification.Id,
            notification.PriceAlertId,
            notification.ProductId,
            notification.PriceHistoryId,
            notification.TriggeredPrice,
            notification.TargetPrice,
            notification.TriggeredAt,
            notification.CreatedAtUtc,
            notification.UpdatedAtUtc);
    }
}
