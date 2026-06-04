namespace PriceWise.Application.Authentication.Dtos;

public sealed record CurrentUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAtUtc);
