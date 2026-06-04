namespace PriceWise.Infrastructure.Notifications;

public sealed class WebhookNotificationOptions
{
    public const string SectionName = "WebhookNotifications";

    public bool Enabled { get; set; } = true;

    public int TimeoutInSeconds { get; set; } = 10;

    public int MaxRetryAttempts { get; set; } = 3;
}
