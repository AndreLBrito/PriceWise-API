using MimeKit;

namespace PriceWise.Infrastructure.Notifications;

public interface ISmtpEmailClient
{
    Task SendAsync(
        MimeMessage message,
        EmailNotificationOptions options,
        CancellationToken cancellationToken = default);
}
