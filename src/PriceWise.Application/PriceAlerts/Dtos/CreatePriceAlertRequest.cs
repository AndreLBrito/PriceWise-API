namespace PriceWise.Application.PriceAlerts.Dtos;

public sealed record CreatePriceAlertRequest(Guid ProductId, decimal TargetPrice);
