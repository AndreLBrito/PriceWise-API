namespace PriceWise.Application.Dashboard.Dtos;

public sealed record DashboardSummaryResponse(
    int TotalProducts,
    int ActiveProducts,
    int InactiveProducts,
    int TotalStores,
    int ActiveStores,
    int InactiveStores,
    int TotalPriceHistories,
    int TotalPriceAlerts,
    int ActivePriceAlerts,
    int TotalAlertNotifications,
    decimal? LowestPriceRegistered,
    decimal? HighestPriceRegistered,
    decimal? AveragePriceRegistered,
    DateTime? LastPriceCapturedAt);
