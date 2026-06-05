using PriceWise.Domain.Enums;

namespace PriceWise.Application.Outbox.Dtos;

public sealed record OutboxMessageResponse(
    Guid Id,
    string Type,
    OutboxMessageStatus Status,
    int RetryCount,
    int MaxRetries,
    DateTime NextAttemptAt,
    DateTime? ProcessedAt,
    string? ErrorMessage,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
