namespace PriceWise.Application.PriceHistories.Dtos;

public sealed record PriceHistoryResponse(
    Guid Id,
    Guid ProductId,
    Guid StoreId,
    decimal Price,
    string Currency,
    DateTime CapturedAt,
    string? SourceUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
