using PriceWise.Domain.Enums;

namespace PriceWise.Application.Outbox.Dtos;

public sealed record OutboxNotificationPayload(
    Guid NotificationId,
    Guid UserId,
    Guid PriceAlertId,
    Guid ProductId,
    Guid PriceHistoryId,
    decimal TriggeredPrice,
    decimal TargetPrice,
    DateTime TriggeredAt,
    Guid ChannelId,
    NotificationChannelType ChannelType,
    string ChannelName,
    string ChannelDestination,
    bool ChannelIsActive,
    DateTime NotificationCreatedAtUtc,
    DateTime ChannelCreatedAtUtc);
