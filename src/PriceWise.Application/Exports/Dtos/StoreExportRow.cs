namespace PriceWise.Application.Exports.Dtos;

public sealed record StoreExportRow(
    Guid Id,
    string Name,
    string BaseUrl,
    string? LogoUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
