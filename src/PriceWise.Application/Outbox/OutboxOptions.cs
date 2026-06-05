namespace PriceWise.Application.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;

    public int IntervalInSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 5;

    public int BatchSize { get; set; } = 20;
}
