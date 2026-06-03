namespace PriceWise.Application.Stores.Dtos;

public sealed record UpdateStoreRequest(string Name, string BaseUrl, string? LogoUrl);
