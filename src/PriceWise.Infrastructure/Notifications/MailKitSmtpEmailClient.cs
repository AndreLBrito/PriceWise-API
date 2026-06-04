using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PriceWise.Infrastructure.Notifications;

public sealed class MailKitSmtpEmailClient : ISmtpEmailClient
{
    public async Task SendAsync(
        MimeMessage message,
        EmailNotificationOptions options,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient
        {
            Timeout = Math.Max(1, options.TimeoutInSeconds) * 1000
        };

        var secureSocketOptions = options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.None;

        await client.ConnectAsync(options.Host, options.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            await client.AuthenticateAsync(options.UserName, options.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
