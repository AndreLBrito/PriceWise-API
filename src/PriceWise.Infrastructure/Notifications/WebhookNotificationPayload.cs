namespace PriceWise.Infrastructure.Notifications;

public sealed record WebhookNotificationPayload(
    Guid NotificationId,
    Guid UserId,
    Guid ProductId,
    Guid PriceAlertId,
    Guid PriceHistoryId,
    string ProductName,
    decimal TargetPrice,
    decimal TriggeredPrice,
    DateTime TriggeredAt,
    string Message);
