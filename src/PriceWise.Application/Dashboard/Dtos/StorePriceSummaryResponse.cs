namespace PriceWise.Application.Dashboard.Dtos;

public sealed record StorePriceSummaryResponse(
    Guid StoreId,
    string StoreName,
    int TotalProductsMonitored,
    int TotalPriceHistories,
    decimal? LowestPriceRegistered,
    decimal? HighestPriceRegistered,
    decimal? AveragePriceRegistered,
    DateTime? LastPriceCapturedAt);
