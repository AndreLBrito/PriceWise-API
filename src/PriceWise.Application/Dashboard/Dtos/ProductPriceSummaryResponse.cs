namespace PriceWise.Application.Dashboard.Dtos;

public sealed record ProductPriceSummaryResponse(
    Guid ProductId,
    string ProductName,
    int TotalPriceHistories,
    decimal? LowestPrice,
    decimal? HighestPrice,
    decimal? AveragePrice,
    decimal? LatestPrice,
    DateTime? LatestPriceCapturedAt,
    DateTime? FirstPriceCapturedAt,
    decimal? PriceVariationPercentage,
    bool HasActiveAlert,
    decimal? TargetAlertPrice);
