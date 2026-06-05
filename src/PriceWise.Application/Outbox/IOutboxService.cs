using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.Outbox.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Outbox;

public interface IOutboxService : IService
{
    Task EnqueueNotificationAsync(
        AlertNotification notification,
        NotificationChannel channel,
        CancellationToken cancellationToken = default);

    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);

    Task<Result<OutboxMessageResponse>> RetryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<OutboxMessageResponse>>> ListAsync(
        OutboxListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OutboxMessageResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
