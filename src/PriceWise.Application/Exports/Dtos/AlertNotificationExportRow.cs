namespace PriceWise.Application.Exports.Dtos;

public sealed record AlertNotificationExportRow(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid PriceAlertId,
    Guid PriceHistoryId,
    decimal TriggeredPrice,
    decimal TargetPrice,
    DateTime TriggeredAt,
    DateTime CreatedAtUtc);
