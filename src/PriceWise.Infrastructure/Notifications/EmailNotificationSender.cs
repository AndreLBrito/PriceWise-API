using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Domain.Enums;

namespace PriceWise.Infrastructure.Notifications;

public sealed class EmailNotificationSender : IEmailNotificationSender
{
    private readonly ISmtpEmailClient smtpEmailClient;
    private readonly IProductRepository productRepository;
    private readonly IOptions<EmailNotificationOptions> options;
    private readonly ILogger<EmailNotificationSender> logger;
    private readonly IApplicationTelemetry telemetry;

    public EmailNotificationSender(
        ISmtpEmailClient smtpEmailClient,
        IProductRepository productRepository,
        IOptions<EmailNotificationOptions> options,
        ILogger<EmailNotificationSender> logger,
        IApplicationTelemetry telemetry)
    {
        this.smtpEmailClient = smtpEmailClient;
        this.productRepository = productRepository;
        this.options = options;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("EmailNotificationSender.Send");
        var emailOptions = Normalize(options.Value);

        if (!emailOptions.Enabled)
        {
            logger.LogInformation(
                "Envio de e-mail desabilitado. Notificação {NotificationId} não enviada.",
                delivery.AlertNotification.Id);
            return;
        }

        if (!delivery.Channel.IsActive || delivery.Channel.Type != NotificationChannelType.Email)
        {
            logger.LogInformation(
                "Canal de e-mail inativo ou inválido. Notificação {NotificationId} não enviada para o canal {ChannelId}.",
                delivery.AlertNotification.Id,
                delivery.Channel.Id);
            return;
        }

        var payload = await CreatePayloadAsync(delivery, cancellationToken);
        var message = CreateMessage(payload, emailOptions);

        for (var attempt = 1; attempt <= emailOptions.MaxRetryAttempts; attempt++)
        {
            try
            {
                await smtpEmailClient.SendAsync(message, emailOptions, cancellationToken);
                logger.LogInformation(
                    "E-mail enviado com sucesso para a notificação {NotificationId}. Canal: {ChannelId}, Destino: {Destination}",
                    payload.NotificationId,
                    delivery.Channel.Id,
                    delivery.Channel.Destination);
                return;
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                telemetry.RecordError(exception);
                logger.LogWarning(
                    exception,
                    "Timeout ao enviar e-mail para a notificação {NotificationId}. Canal: {ChannelId}, Tentativa: {Attempt}",
                    payload.NotificationId,
                    delivery.Channel.Id,
                    attempt);

                if (attempt == emailOptions.MaxRetryAttempts)
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                telemetry.RecordError(exception);
                logger.LogWarning(
                    exception,
                    "Falha ao enviar e-mail para a notificação {NotificationId}. Canal: {ChannelId}, Tentativa: {Attempt}",
                    payload.NotificationId,
                    delivery.Channel.Id,
                    attempt);

                if (attempt == emailOptions.MaxRetryAttempts)
                {
                    return;
                }
            }
        }
    }

    private async Task<EmailNotificationPayload> CreatePayloadAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        var notification = delivery.AlertNotification;
        var product = await productRepository.GetByIdAsync(
            notification.ProductId,
            notification.UserId,
            cancellationToken);
        var productName = product?.Name ?? "Produto monitorado";

        return new EmailNotificationPayload(
            notification.Id,
            notification.UserId,
            notification.ProductId,
            notification.PriceAlertId,
            notification.PriceHistoryId,
            productName,
            product?.ProductUrl,
            notification.TargetPrice,
            notification.TriggeredPrice,
            notification.TriggeredAt,
            delivery.Channel.Destination,
            $"Alerta de preço: {productName}");
    }

    private static MimeMessage CreateMessage(
        EmailNotificationPayload payload,
        EmailNotificationOptions options)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
        message.To.Add(MailboxAddress.Parse(payload.Destination));
        message.Subject = payload.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = EmailNotificationTemplate.BuildText(payload),
            HtmlBody = EmailNotificationTemplate.BuildHtml(payload)
        };

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private static EmailNotificationOptions Normalize(EmailNotificationOptions options)
    {
        return new EmailNotificationOptions
        {
            Enabled = options.Enabled,
            Host = string.IsNullOrWhiteSpace(options.Host) ? "localhost" : options.Host,
            Port = options.Port <= 0 ? 1025 : options.Port,
            UseSsl = options.UseSsl,
            UserName = options.UserName,
            Password = options.Password,
            FromName = string.IsNullOrWhiteSpace(options.FromName) ? "PriceWise" : options.FromName,
            FromEmail = string.IsNullOrWhiteSpace(options.FromEmail) ? "noreply@pricewise.local" : options.FromEmail,
            TimeoutInSeconds = Math.Max(1, options.TimeoutInSeconds),
            MaxRetryAttempts = Math.Max(1, options.MaxRetryAttempts)
        };
    }
}
