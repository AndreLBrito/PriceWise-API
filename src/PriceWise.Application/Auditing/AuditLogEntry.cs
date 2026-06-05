namespace PriceWise.Application.Auditing;

public sealed record AuditLogEntry(
    Guid? UserId,
    string Action,
    string EntityName,
    Guid? EntityId,
    object? OldValues = null,
    object? NewValues = null);
