namespace PriceWise.Application.Authentication.Dtos;

public sealed record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);
