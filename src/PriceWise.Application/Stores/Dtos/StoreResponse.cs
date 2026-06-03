namespace PriceWise.Application.Stores.Dtos;

public sealed record StoreResponse(
    Guid Id,
    string Name,
    string BaseUrl,
    string? LogoUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
