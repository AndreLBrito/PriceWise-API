using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Auditing;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Auditing;
using PriceWise.Application.Common;
using PriceWise.Application.Outbox.Dtos;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.Outbox;

public sealed class OutboxService : IOutboxService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxRepository outboxRepository;
    private readonly IWebhookNotificationSender webhookNotificationSender;
    private readonly IEmailNotificationSender emailNotificationSender;
    private readonly IAuditContext auditContext;
    private readonly IAuditLogService auditLogService;
    private readonly OutboxOptions options;
    private readonly ILogger<OutboxService> logger;
    private readonly IApplicationTelemetry telemetry;

    public OutboxService(
        IOutboxRepository outboxRepository,
        IWebhookNotificationSender webhookNotificationSender,
        IEmailNotificationSender emailNotificationSender,
        IAuditContext auditContext,
        IAuditLogService auditLogService,
        IOptions<OutboxOptions> options,
        ILogger<OutboxService> logger,
        IApplicationTelemetry telemetry)
    {
        this.outboxRepository = outboxRepository;
        this.webhookNotificationSender = webhookNotificationSender;
        this.emailNotificationSender = emailNotificationSender;
        this.auditContext = auditContext;
        this.auditLogService = auditLogService;
        this.options = options.Value;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task EnqueueNotificationAsync(
        AlertNotification notification,
        NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        if (!Normalize(options).Enabled || !channel.IsActive)
        {
            return;
        }

        var type = channel.Type switch
        {
            NotificationChannelType.Webhook => OutboxMessageTypes.WebhookNotification,
            NotificationChannelType.Email => OutboxMessageTypes.EmailNotification,
            _ => null
        };

        if (type is null)
        {
            return;
        }

        var payload = new OutboxNotificationPayload(
            notification.Id,
            notification.UserId,
            notification.PriceAlertId,
            notification.ProductId,
            notification.PriceHistoryId,
            notification.TriggeredPrice,
            notification.TargetPrice,
            notification.TriggeredAt,
            channel.Id,
            channel.Type,
            channel.Name,
            channel.Destination,
            channel.IsActive,
            notification.CreatedAtUtc,
            channel.CreatedAtUtc);
        var message = OutboxMessage.Create(
            type,
            JsonSerializer.Serialize(payload, JsonOptions),
            Normalize(options).MaxRetries,
            auditContext.GetCurrent().CorrelationId);

        await outboxRepository.AddAsync(message, cancellationToken);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("OutboxService.ProcessPending");
        var normalizedOptions = Normalize(options);

        if (!normalizedOptions.Enabled)
        {
            return 0;
        }

        var messages = await outboxRepository.ListPendingAsync(
            normalizedOptions.BatchSize,
            DateTime.UtcNow,
            cancellationToken);
        var processed = 0;

        foreach (var message in messages)
        {
            await ProcessAsync(message, cancellationToken);
            processed++;
        }

        return processed;
    }

    public async Task<Result<OutboxMessageResponse>> RetryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await outboxRepository.GetByIdAsync(id, cancellationToken);

        if (message is null)
        {
            return Result<OutboxMessageResponse>.Failure(OutboxErrors.MessageNotFound);
        }

        if (message.Status != OutboxMessageStatus.Failed)
        {
            return Result<OutboxMessageResponse>.Failure(OutboxErrors.RetryOnlyFailedMessages);
        }

        await outboxRepository.ResetFailedAsync(id, DateTime.UtcNow, cancellationToken);
        var updatedMessage = await outboxRepository.GetByIdAsync(id, cancellationToken);

        return Result<OutboxMessageResponse>.Success(MapToResponse(updatedMessage ?? message));
    }

    public async Task<Result<PagedResponse<OutboxMessageResponse>>> ListAsync(
        OutboxListRequest request,
        CancellationToken cancellationToken = default)
    {
        var messages = await outboxRepository.ListAsync(request, cancellationToken);
        var response = PagedResponse<OutboxMessageResponse>.Create(
            messages.Items.Select(MapToResponse).ToArray(),
            messages.Page,
            messages.PageSize,
            messages.TotalItems);

        return Result<PagedResponse<OutboxMessageResponse>>.Success(response);
    }

    public async Task<Result<OutboxMessageResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await outboxRepository.GetByIdAsync(id, cancellationToken);

        return message is null
            ? Result<OutboxMessageResponse>.Failure(OutboxErrors.MessageNotFound)
            : Result<OutboxMessageResponse>.Success(MapToResponse(message));
    }

    private async Task ProcessAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await outboxRepository.MarkProcessingAsync(message.Id, cancellationToken);
            var delivery = CreateDelivery(message);

            if (message.Type == OutboxMessageTypes.WebhookNotification)
            {
                await webhookNotificationSender.SendAsync(delivery, cancellationToken);
            }
            else if (message.Type == OutboxMessageTypes.EmailNotification)
            {
                await emailNotificationSender.SendAsync(delivery, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported outbox message type: {message.Type}");
            }

            var processedAt = DateTime.UtcNow;
            await outboxRepository.MarkProcessedAsync(message.Id, processedAt, cancellationToken);
            await RecordAuditAsync(message, AuditActions.OutboxProcessed, processedAt, null, cancellationToken);
        }
        catch (Exception exception)
        {
            telemetry.RecordError(exception);
            logger.LogWarning(
                exception,
                "Falha ao processar mensagem da outbox {OutboxMessageId}.",
                message.Id);
            await ScheduleRetryOrFailAsync(message, exception, cancellationToken);
        }
    }

    private async Task ScheduleRetryOrFailAsync(
        OutboxMessage message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryCount = message.RetryCount + 1;
        var failed = retryCount >= message.MaxRetries;
        var nextAttemptAt = failed
            ? DateTime.UtcNow
            : DateTime.UtcNow.AddSeconds(Math.Pow(2, retryCount) * 10);
        var status = failed ? OutboxMessageStatus.Failed : OutboxMessageStatus.Pending;
        var errorMessage = exception.Message.Length > 1000
            ? exception.Message[..1000]
            : exception.Message;

        await outboxRepository.ScheduleRetryAsync(
            message.Id,
            retryCount,
            status,
            nextAttemptAt,
            errorMessage,
            cancellationToken);
        await RecordAuditAsync(
            message,
            failed ? AuditActions.OutboxFailed : AuditActions.OutboxRetryScheduled,
            DateTime.UtcNow,
            errorMessage,
            cancellationToken);
    }

    private static NotificationDelivery CreateDelivery(OutboxMessage message)
    {
        var payload = JsonSerializer.Deserialize<OutboxNotificationPayload>(message.Payload, JsonOptions)
            ?? throw new InvalidOperationException("Payload da mensagem da outbox é inválido.");
        var notification = AlertNotification.Restore(
            payload.NotificationId,
            payload.UserId,
            payload.PriceAlertId,
            payload.ProductId,
            payload.PriceHistoryId,
            payload.TriggeredPrice,
            payload.TargetPrice,
            payload.TriggeredAt,
            payload.NotificationCreatedAtUtc,
            null);
        var channel = NotificationChannel.Restore(
            payload.ChannelId,
            payload.UserId,
            payload.ChannelType,
            payload.ChannelName,
            payload.ChannelDestination,
            payload.ChannelIsActive,
            payload.ChannelCreatedAtUtc,
            null);

        return new NotificationDelivery(notification, channel);
    }

    private async Task RecordAuditAsync(
        OutboxMessage message,
        string action,
        DateTime occurredAt,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditLogService.RecordAsync(new AuditLogEntry(
                null,
                action,
                "OutboxMessage",
                message.Id,
                NewValues: new
                {
                    message.Type,
                    message.Status,
                    message.RetryCount,
                    ErrorMessage = errorMessage,
                    OccurredAt = occurredAt
                }), cancellationToken);
        }
        catch (Exception auditException)
        {
            logger.LogWarning(auditException, "Não foi possível registrar auditoria da outbox.");
        }
    }

    private static OutboxMessageResponse MapToResponse(OutboxMessage message)
    {
        return new OutboxMessageResponse(
            message.Id,
            message.Type,
            message.Status,
            message.RetryCount,
            message.MaxRetries,
            message.NextAttemptAt,
            message.ProcessedAt,
            message.ErrorMessage,
            message.CorrelationId,
            message.CreatedAtUtc,
            message.UpdatedAtUtc);
    }

    private static OutboxOptions Normalize(OutboxOptions options)
    {
        return new OutboxOptions
        {
            Enabled = options.Enabled,
            IntervalInSeconds = Math.Max(5, options.IntervalInSeconds),
            MaxRetries = Math.Max(1, options.MaxRetries),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100)
        };
    }
}
