namespace PriceWise.Application.PriceAlerts.Dtos;

public sealed record PriceAlertResponse(
    Guid Id,
    Guid ProductId,
    decimal TargetPrice,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
