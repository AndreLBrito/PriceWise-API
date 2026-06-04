namespace PriceWise.Application.Exports.Dtos;

public sealed record PriceHistoryExportRow(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid StoreId,
    string StoreName,
    decimal Price,
    string Currency,
    DateTime CapturedAt,
    string? SourceUrl,
    DateTime CreatedAtUtc);
