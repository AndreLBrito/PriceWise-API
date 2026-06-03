namespace PriceWise.Application.PriceHistories.Dtos;

public sealed record CreatePriceHistoryRequest(
    Guid ProductId,
    Guid StoreId,
    decimal Price,
    string Currency,
    DateTime? CapturedAt,
    string? SourceUrl);
