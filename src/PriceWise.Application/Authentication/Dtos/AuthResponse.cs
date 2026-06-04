namespace PriceWise.Application.Authentication.Dtos;

public sealed record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);
