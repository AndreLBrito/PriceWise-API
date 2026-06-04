namespace PriceWise.Infrastructure.Notifications;

public sealed record EmailNotificationPayload(
    Guid NotificationId,
    Guid UserId,
    Guid ProductId,
    Guid PriceAlertId,
    Guid PriceHistoryId,
    string ProductName,
    string? ProductUrl,
    decimal TargetPrice,
    decimal TriggeredPrice,
    DateTime TriggeredAt,
    string Destination,
    string Subject);
