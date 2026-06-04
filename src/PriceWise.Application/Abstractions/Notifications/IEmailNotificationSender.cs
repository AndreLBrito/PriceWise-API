namespace PriceWise.Application.Abstractions.Notifications;

public interface IEmailNotificationSender
{
    Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);
}
