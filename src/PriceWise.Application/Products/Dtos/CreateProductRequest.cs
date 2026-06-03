namespace PriceWise.Application.Products.Dtos;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    string? Brand,
    string? Category,
    string ProductUrl,
    string? ImageUrl);
