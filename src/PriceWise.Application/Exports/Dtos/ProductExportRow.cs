namespace PriceWise.Application.Exports.Dtos;

public sealed record ProductExportRow(
    Guid Id,
    string Name,
    string? Description,
    string? Brand,
    string? Category,
    string ProductUrl,
    string? ImageUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
