namespace PriceWise.Application.Abstractions.Notifications;

public interface IWebhookNotificationSender
{
    Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);
}
