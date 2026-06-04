namespace PriceWise.Application.Dashboard.Dtos;

public sealed record AlertSummaryResponse(
    int TotalAlerts,
    int ActiveAlerts,
    int InactiveAlerts,
    int TotalNotifications,
    int NotificationsLastSevenDays,
    int NotificationsLastThirtyDays,
    DateTime? LastNotificationAt);
