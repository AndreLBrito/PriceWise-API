using Microsoft.Extensions.Logging;
using PriceWise.Application.Abstractions.Notifications;

namespace PriceWise.Infrastructure.Notifications;

public sealed class EmailNotificationSender : IEmailNotificationSender
{
    private readonly ILogger<EmailNotificationSender> logger;

    public EmailNotificationSender(ILogger<EmailNotificationSender> logger)
    {
        this.logger = logger;
    }

    public Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Canal Email notificado para o alerta {PriceAlertId}. Canal: {ChannelName}, Destino: {Destination}, Preço disparado: {TriggeredPrice}, Preço alvo: {TargetPrice}",
            delivery.AlertNotification.PriceAlertId,
            delivery.Channel.Name,
            delivery.Channel.Destination,
            delivery.AlertNotification.TriggeredPrice,
            delivery.AlertNotification.TargetPrice);

        return Task.CompletedTask;
    }
}
