namespace PriceWise.Infrastructure.Notifications;

public sealed class EmailNotificationOptions
{
    public const string SectionName = "EmailNotifications";

    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool UseSsl { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromName { get; set; } = "PriceWise";

    public string FromEmail { get; set; } = "noreply@pricewise.local";

    public int TimeoutInSeconds { get; set; } = 10;

    public int MaxRetryAttempts { get; set; } = 3;
}
