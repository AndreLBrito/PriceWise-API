namespace PriceWise.Application.AlertNotifications.Dtos;

public sealed record AlertNotificationResponse(
    Guid Id,
    Guid PriceAlertId,
    Guid ProductId,
    Guid PriceHistoryId,
    decimal TriggeredPrice,
    decimal TargetPrice,
    DateTime TriggeredAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
