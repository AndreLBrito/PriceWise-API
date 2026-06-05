using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    private AuditLog(
        Guid id,
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        SetCreatedAt(createdAtUtc);
    }

    private AuditLog(
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
    }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; }

    public string EntityName { get; private set; }

    public Guid? EntityId { get; private set; }

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? CorrelationId { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        return new AuditLog(
            userId,
            action,
            entityName,
            entityId,
            oldValues,
            newValues,
            ipAddress,
            userAgent,
            correlationId);
    }

    public static AuditLog Restore(
        Guid id,
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        DateTime createdAtUtc)
    {
        return new AuditLog(
            id,
            userId,
            action,
            entityName,
            entityId,
            oldValues,
            newValues,
            ipAddress,
            userAgent,
            correlationId,
            createdAtUtc);
    }
}
