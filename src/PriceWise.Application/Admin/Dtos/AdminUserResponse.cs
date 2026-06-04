namespace PriceWise.Application.Admin.Dtos;

public sealed record AdminUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    int FailedLoginAttempts,
    DateTime? LockedUntilUtc,
    DateTime CreatedAtUtc);
