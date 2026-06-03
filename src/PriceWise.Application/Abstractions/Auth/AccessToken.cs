namespace PriceWise.Application.Abstractions.Auth;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
