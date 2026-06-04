using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Domain.Enums;

namespace PriceWise.Infrastructure.Notifications;

public sealed class WebhookNotificationSender : IWebhookNotificationSender
{
    private readonly HttpClient httpClient;
    private readonly IProductRepository productRepository;
    private readonly IOptions<WebhookNotificationOptions> options;
    private readonly ILogger<WebhookNotificationSender> logger;
    private readonly IApplicationTelemetry telemetry;

    public WebhookNotificationSender(
        HttpClient httpClient,
        IProductRepository productRepository,
        IOptions<WebhookNotificationOptions> options,
        ILogger<WebhookNotificationSender> logger,
        IApplicationTelemetry telemetry)
    {
        this.httpClient = httpClient;
        this.productRepository = productRepository;
        this.options = options;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("WebhookNotificationSender.Send");
        var webhookOptions = Normalize(options.Value);

        if (!webhookOptions.Enabled)
        {
            logger.LogInformation(
                "Envio de Webhook desabilitado. Notificação {NotificationId} não enviada.",
                delivery.AlertNotification.Id);
            return;
        }

        if (!delivery.Channel.IsActive || delivery.Channel.Type != NotificationChannelType.Webhook)
        {
            logger.LogInformation(
                "Canal Webhook inativo ou inválido. Notificação {NotificationId} não enviada para o canal {ChannelId}.",
                delivery.AlertNotification.Id,
                delivery.Channel.Id);
            return;
        }

        var payload = await CreatePayloadAsync(delivery, cancellationToken);

        for (var attempt = 1; attempt <= webhookOptions.MaxRetryAttempts; attempt++)
        {
            try
            {
                using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(webhookOptions.TimeoutInSeconds));

                var response = await httpClient.PostAsJsonAsync(
                    delivery.Channel.Destination,
                    payload,
                    timeoutCancellationTokenSource.Token);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "Webhook enviado com sucesso para a notificação {NotificationId}. Canal: {ChannelId}, StatusCode: {StatusCode}",
                        payload.NotificationId,
                        delivery.Channel.Id,
                        (int)response.StatusCode);
                    return;
                }

                if (!IsTransient(response.StatusCode) || attempt == webhookOptions.MaxRetryAttempts)
                {
                    logger.LogWarning(
                        "Falha ao enviar Webhook para a notificação {NotificationId}. Canal: {ChannelId}, StatusCode: {StatusCode}, Tentativa: {Attempt}",
                        payload.NotificationId,
                        delivery.Channel.Id,
                        (int)response.StatusCode,
                        attempt);
                    return;
                }
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                telemetry.RecordError(exception);
                logger.LogWarning(
                    exception,
                    "Timeout ao enviar Webhook para a notificação {NotificationId}. Canal: {ChannelId}, Tentativa: {Attempt}",
                    payload.NotificationId,
                    delivery.Channel.Id,
                    attempt);

                if (attempt == webhookOptions.MaxRetryAttempts)
                {
                    return;
                }
            }
            catch (HttpRequestException exception)
            {
                telemetry.RecordError(exception);
                logger.LogWarning(
                    exception,
                    "Falha transitória ao enviar Webhook para a notificação {NotificationId}. Canal: {ChannelId}, Tentativa: {Attempt}",
                    payload.NotificationId,
                    delivery.Channel.Id,
                    attempt);

                if (attempt == webhookOptions.MaxRetryAttempts)
                {
                    return;
                }
            }
        }
    }

    private async Task<WebhookNotificationPayload> CreatePayloadAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        var notification = delivery.AlertNotification;
        var product = await productRepository.GetByIdAsync(
            notification.ProductId,
            notification.UserId,
            cancellationToken);
        var productName = product?.Name ?? "Produto monitorado";

        return new WebhookNotificationPayload(
            notification.Id,
            notification.UserId,
            notification.ProductId,
            notification.PriceAlertId,
            notification.PriceHistoryId,
            productName,
            notification.TargetPrice,
            notification.TriggeredPrice,
            notification.TriggeredAt,
            $"O produto {productName} atingiu o preço de {notification.TriggeredPrice:C}. Alvo configurado: {notification.TargetPrice:C}.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;
    }

    private static WebhookNotificationOptions Normalize(WebhookNotificationOptions options)
    {
        return new WebhookNotificationOptions
        {
            Enabled = options.Enabled,
            TimeoutInSeconds = Math.Max(1, options.TimeoutInSeconds),
            MaxRetryAttempts = Math.Max(1, options.MaxRetryAttempts)
        };
    }
}
