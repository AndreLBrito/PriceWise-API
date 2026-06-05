using PriceWise.Domain.Common;
using PriceWise.Domain.Enums;

namespace PriceWise.Domain.Entities;

public sealed class OutboxMessage : BaseEntity
{
    private OutboxMessage(
        Guid id,
        string type,
        string payload,
        OutboxMessageStatus status,
        int retryCount,
        int maxRetries,
        DateTime nextAttemptAt,
        DateTime? processedAt,
        string? errorMessage,
        string? correlationId,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        Status = status;
        RetryCount = retryCount;
        MaxRetries = maxRetries;
        NextAttemptAt = nextAttemptAt;
        ProcessedAt = processedAt;
        ErrorMessage = errorMessage;
        CorrelationId = correlationId;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private OutboxMessage(
        string type,
        string payload,
        int maxRetries,
        string? correlationId)
    {
        Type = type;
        Payload = payload;
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
        MaxRetries = Math.Max(1, maxRetries);
        NextAttemptAt = DateTime.UtcNow;
        CorrelationId = correlationId;
    }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public int MaxRetries { get; private set; }

    public DateTime NextAttemptAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? CorrelationId { get; private set; }

    public static OutboxMessage Create(
        string type,
        string payload,
        int maxRetries,
        string? correlationId)
    {
        return new OutboxMessage(type.Trim(), payload, maxRetries, correlationId);
    }

    public static OutboxMessage Restore(
        Guid id,
        string type,
        string payload,
        OutboxMessageStatus status,
        int retryCount,
        int maxRetries,
        DateTime nextAttemptAt,
        DateTime? processedAt,
        string? errorMessage,
        string? correlationId,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new OutboxMessage(
            id,
            type,
            payload,
            status,
            retryCount,
            maxRetries,
            nextAttemptAt,
            processedAt,
            errorMessage,
            correlationId,
            createdAtUtc,
            updatedAtUtc);
    }
}
