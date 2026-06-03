namespace PriceWise.Application.Stores.Dtos;

public sealed record CreateStoreRequest(string Name, string BaseUrl, string? LogoUrl);
