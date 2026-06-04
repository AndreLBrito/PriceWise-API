using Microsoft.Extensions.Logging;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.AlertNotifications.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.AlertNotifications;

public sealed class AlertNotificationService : IAlertNotificationService
{
    private readonly IAlertNotificationRepository alertNotificationRepository;
    private readonly IPriceAlertRepository priceAlertRepository;
    private readonly INotificationChannelRepository notificationChannelRepository;
    private readonly IWebhookNotificationSender webhookNotificationSender;
    private readonly IEmailNotificationSender emailNotificationSender;
    private readonly ILogger<AlertNotificationService> logger;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;
    private readonly IApplicationTelemetry telemetry;

    public AlertNotificationService(
        IAlertNotificationRepository alertNotificationRepository,
        IPriceAlertRepository priceAlertRepository,
        INotificationChannelRepository notificationChannelRepository,
        IWebhookNotificationSender webhookNotificationSender,
        IEmailNotificationSender emailNotificationSender,
        ILogger<AlertNotificationService> logger,
        IDashboardCacheInvalidator dashboardCacheInvalidator,
        IApplicationTelemetry telemetry)
    {
        this.alertNotificationRepository = alertNotificationRepository;
        this.priceAlertRepository = priceAlertRepository;
        this.notificationChannelRepository = notificationChannelRepository;
        this.webhookNotificationSender = webhookNotificationSender;
        this.emailNotificationSender = emailNotificationSender;
        this.logger = logger;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
        this.telemetry = telemetry;
    }

    public async Task CheckPriceAlertsAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AlertNotificationService.CheckPriceAlerts");
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
            await dashboardCacheInvalidator.InvalidateAlertSummaryAsync(notification.UserId, cancellationToken);
            telemetry.RecordAlertNotificationCreated();
            await SendNotificationAsync(notification, cancellationToken);
        }
    }

    public async Task<Result<IReadOnlyCollection<AlertNotificationResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AlertNotificationService.List");
        var notifications = await alertNotificationRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = notifications.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<AlertNotificationResponse>>.Success(response);
    }

    public async Task<Result<AlertNotificationResponse>> GetByIdAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("AlertNotificationService.GetById");
        var notification = await alertNotificationRepository.GetByIdAsync(
            notificationId,
            userId,
            cancellationToken);

        if (notification is null)
        {
            telemetry.RecordError(AlertNotificationErrors.AlertNotificationNotFound.Code);
            return Result<AlertNotificationResponse>.Failure(AlertNotificationErrors.AlertNotificationNotFound);
        }

        return Result<AlertNotificationResponse>.Success(MapToResponse(notification));
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

    private async Task SendNotificationAsync(
        AlertNotification notification,
        CancellationToken cancellationToken)
    {
        var channels = await notificationChannelRepository.ListActiveByUserIdAsync(
            notification.UserId,
            cancellationToken);

        foreach (var channel in channels)
        {
            try
            {
                var delivery = new NotificationDelivery(notification, channel);

                if (channel.Type == NotificationChannelType.Webhook)
                {
                    await webhookNotificationSender.SendAsync(delivery, cancellationToken);
                    continue;
                }

                if (channel.Type == NotificationChannelType.Email)
                {
                    await emailNotificationSender.SendAsync(delivery, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                telemetry.RecordError(exception);
                logger.LogWarning(
                    exception,
                    "Falha ao notificar o canal {ChannelId} para a notificação de alerta {AlertNotificationId}.",
                    channel.Id,
                    notification.Id);
            }
        }
    }
}
