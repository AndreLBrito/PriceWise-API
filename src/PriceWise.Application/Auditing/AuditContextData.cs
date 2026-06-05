namespace PriceWise.Application.Auditing;

public sealed record AuditContextData(
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);
