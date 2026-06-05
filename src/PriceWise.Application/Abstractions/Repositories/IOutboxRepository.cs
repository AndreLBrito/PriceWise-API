using PriceWise.Application.Common;
using PriceWise.Application.Outbox.Dtos;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IOutboxRepository : IRepository<OutboxMessage>
{
    Task<IReadOnlyCollection<OutboxMessage>> ListPendingAsync(
        int batchSize,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<OutboxMessage>> ListAsync(
        OutboxListRequest request,
        CancellationToken cancellationToken = default);

    Task MarkProcessingAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(
        Guid id,
        DateTime processedAt,
        CancellationToken cancellationToken = default);

    Task ScheduleRetryAsync(
        Guid id,
        int retryCount,
        OutboxMessageStatus status,
        DateTime nextAttemptAt,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task ResetFailedAsync(
        Guid id,
        DateTime nextAttemptAt,
        CancellationToken cancellationToken = default);
}
