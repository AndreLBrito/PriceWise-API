namespace PriceWise.Application.Products.Dtos;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Brand,
    string? Category,
    string ProductUrl,
    string? ImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
