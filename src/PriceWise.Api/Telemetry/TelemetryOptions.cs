namespace PriceWise.Api.Telemetry;

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; set; } = true;

    public string ServiceName { get; set; } = "PriceWise.Api";

    public string ServiceVersion { get; set; } = "1.0.0";

    public string Exporter { get; set; } = "Console";

    public string? OtlpEndpoint { get; set; }

    public bool EnableMetrics { get; set; } = true;

    public bool EnableTracing { get; set; } = true;
}
