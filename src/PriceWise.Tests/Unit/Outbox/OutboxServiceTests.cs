using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Auditing;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Auditing;
using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;
using PriceWise.Application.Outbox;
using PriceWise.Application.Outbox.Dtos;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Tests.Unit.Outbox;

public sealed class OutboxServiceTests
{
    [Fact]
    public async Task EnqueueNotificationAsyncCreatesOutboxMessage()
    {
        var repository = new InMemoryOutboxRepository();
        var service = CreateService(repository);
        var notification = CreateNotification();
        var channel = CreateChannel(notification.UserId, NotificationChannelType.Webhook);

        await service.EnqueueNotificationAsync(notification, channel);

        repository.Messages.Should().ContainSingle();
        repository.Messages[0].Type.Should().Be(OutboxMessageTypes.WebhookNotification);
        repository.Messages[0].CorrelationId.Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task ProcessPendingAsyncProcessesPendingMessage()
    {
        var repository = new InMemoryOutboxRepository();
        var webhookSender = new SpyWebhookSender();
        var service = CreateService(repository, webhookSender: webhookSender);
        var notification = CreateNotification();
        var channel = CreateChannel(notification.UserId, NotificationChannelType.Webhook);
        await service.EnqueueNotificationAsync(notification, channel);

        var processed = await service.ProcessPendingAsync();

        processed.Should().Be(1);
        webhookSender.Deliveries.Should().ContainSingle();
        repository.Messages[0].Status.Should().Be(OutboxMessageStatus.Processed);
        repository.Messages[0].ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingAsyncSchedulesRetryWhenSenderFails()
    {
        var repository = new InMemoryOutboxRepository();
        var webhookSender = new SpyWebhookSender(new InvalidOperationException("Falha externa"));
        var service = CreateService(repository, webhookSender: webhookSender);
        var notification = CreateNotification();
        var channel = CreateChannel(notification.UserId, NotificationChannelType.Webhook);
        await service.EnqueueNotificationAsync(notification, channel);

        await service.ProcessPendingAsync();

        repository.Messages[0].Status.Should().Be(OutboxMessageStatus.Pending);
        repository.Messages[0].RetryCount.Should().Be(1);
        repository.Messages[0].NextAttemptAt.Should().BeAfter(DateTime.UtcNow);
        repository.Messages[0].ErrorMessage.Should().Be("Falha externa");
    }

    [Fact]
    public async Task ProcessPendingAsyncMarksFailedWhenMaxRetriesIsReached()
    {
        var repository = new InMemoryOutboxRepository();
        var webhookSender = new SpyWebhookSender(new InvalidOperationException("Falha final"));
        var service = CreateService(repository, webhookSender: webhookSender, maxRetries: 1);
        var notification = CreateNotification();
        var channel = CreateChannel(notification.UserId, NotificationChannelType.Webhook);
        await service.EnqueueNotificationAsync(notification, channel);

        await service.ProcessPendingAsync();

        repository.Messages[0].Status.Should().Be(OutboxMessageStatus.Failed);
        repository.Messages[0].RetryCount.Should().Be(1);
        repository.Messages[0].ErrorMessage.Should().Be("Falha final");
    }

    [Fact]
    public async Task RetryAsyncReactivatesFailedMessage()
    {
        var repository = new InMemoryOutboxRepository();
        var service = CreateService(repository, maxRetries: 1);
        var notification = CreateNotification();
        var channel = CreateChannel(notification.UserId, NotificationChannelType.Webhook);
        await service.EnqueueNotificationAsync(notification, channel);
        await repository.ScheduleRetryAsync(
            repository.Messages[0].Id,
            1,
            OutboxMessageStatus.Failed,
            DateTime.UtcNow,
            "Falha final");

        var result = await service.RetryAsync(repository.Messages[0].Id);

        result.IsSuccess.Should().BeTrue();
        repository.Messages[0].Status.Should().Be(OutboxMessageStatus.Pending);
        repository.Messages[0].ErrorMessage.Should().BeNull();
    }

    private static OutboxService CreateService(
        InMemoryOutboxRepository repository,
        IWebhookNotificationSender? webhookSender = null,
        IEmailNotificationSender? emailSender = null,
        int maxRetries = 5)
    {
        return new OutboxService(
            repository,
            webhookSender ?? new SpyWebhookSender(),
            emailSender ?? new SpyEmailSender(),
            new FakeAuditContext(),
            new FakeAuditLogService(),
            Options.Create(new OutboxOptions
            {
                Enabled = true,
                IntervalInSeconds = 30,
                MaxRetries = maxRetries,
                BatchSize = 20
            }),
            NullLogger<OutboxService>.Instance,
            new NoOpApplicationTelemetry());
    }

    private static AlertNotification CreateNotification()
    {
        return AlertNotification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            90m,
            100m);
    }

    private static NotificationChannel CreateChannel(Guid userId, NotificationChannelType type)
    {
        return NotificationChannel.Create(
            userId,
            type,
            type.ToString(),
            type == NotificationChannelType.Webhook ? "https://example.com/webhook" : "user@example.com");
    }

    private sealed class InMemoryOutboxRepository : IOutboxRepository
    {
        public List<OutboxMessage> Messages { get; } = [];

        public Task<IReadOnlyCollection<OutboxMessage>> ListPendingAsync(
            int batchSize,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<OutboxMessage> messages = Messages
                .Where(message => message.Status == OutboxMessageStatus.Pending && message.NextAttemptAt <= utcNow)
                .OrderBy(message => message.NextAttemptAt)
                .Take(batchSize)
                .ToArray();

            return Task.FromResult(messages);
        }

        public Task<PagedResponse<OutboxMessage>> ListAsync(
            OutboxListRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PagedResponse<OutboxMessage>.Create(
                Messages,
                request.Page,
                request.PageSize,
                Messages.Count));
        }

        public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Messages.SingleOrDefault(message => message.Id == id));
        }

        public Task AddAsync(OutboxMessage entity, CancellationToken cancellationToken = default)
        {
            Messages.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(OutboxMessage entity, CancellationToken cancellationToken = default)
        {
            Replace(entity);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Messages.RemoveAll(message => message.Id == id);

            return Task.CompletedTask;
        }

        public Task MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var message = Messages.Single(item => item.Id == id);
            Replace(message, status: OutboxMessageStatus.Processing, updatedAtUtc: DateTime.UtcNow);

            return Task.CompletedTask;
        }

        public Task MarkProcessedAsync(
            Guid id,
            DateTime processedAt,
            CancellationToken cancellationToken = default)
        {
            var message = Messages.Single(item => item.Id == id);
            Replace(
                message,
                status: OutboxMessageStatus.Processed,
                processedAt: processedAt,
                errorMessage: null,
                updatedAtUtc: DateTime.UtcNow);

            return Task.CompletedTask;
        }

        public Task ScheduleRetryAsync(
            Guid id,
            int retryCount,
            OutboxMessageStatus status,
            DateTime nextAttemptAt,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            var message = Messages.Single(item => item.Id == id);
            Replace(
                message,
                status: status,
                retryCount: retryCount,
                nextAttemptAt: nextAttemptAt,
                errorMessage: errorMessage,
                updatedAtUtc: DateTime.UtcNow);

            return Task.CompletedTask;
        }

        public Task ResetFailedAsync(
            Guid id,
            DateTime nextAttemptAt,
            CancellationToken cancellationToken = default)
        {
            var message = Messages.Single(item => item.Id == id);
            Replace(
                message,
                status: OutboxMessageStatus.Pending,
                nextAttemptAt: nextAttemptAt,
                processedAt: null,
                errorMessage: null,
                updatedAtUtc: DateTime.UtcNow);

            return Task.CompletedTask;
        }

        private void Replace(
            OutboxMessage message,
            OutboxMessageStatus? status = null,
            int? retryCount = null,
            DateTime? nextAttemptAt = null,
            DateTime? processedAt = null,
            string? errorMessage = null,
            DateTime? updatedAtUtc = null)
        {
            var replacement = OutboxMessage.Restore(
                message.Id,
                message.Type,
                message.Payload,
                status ?? message.Status,
                retryCount ?? message.RetryCount,
                message.MaxRetries,
                nextAttemptAt ?? message.NextAttemptAt,
                processedAt,
                errorMessage,
                message.CorrelationId,
                message.CreatedAtUtc,
                updatedAtUtc ?? message.UpdatedAtUtc);
            var index = Messages.FindIndex(item => item.Id == message.Id);
            Messages[index] = replacement;
        }
    }

    private sealed class SpyWebhookSender : IWebhookNotificationSender
    {
        private readonly Exception? exception;

        public SpyWebhookSender(Exception? exception = null)
        {
            this.exception = exception;
        }

        public List<NotificationDelivery> Deliveries { get; } = [];

        public Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
        {
            if (exception is not null)
            {
                throw exception;
            }

            Deliveries.Add(delivery);

            return Task.CompletedTask;
        }
    }

    private sealed class SpyEmailSender : IEmailNotificationSender
    {
        public Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditContext : IAuditContext
    {
        public AuditContextData GetCurrent()
        {
            return new AuditContextData(null, null, "test-correlation-id");
        }
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<Result<PagedResponse<AuditLogResponse>>> ListAsync(
            AuditLogListRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<AuditLogResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
