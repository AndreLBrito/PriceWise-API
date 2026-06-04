namespace PriceWise.Api.Features.Telemetry;

public sealed record TelemetryInfoResponse(
    string ServiceName,
    string ServiceVersion,
    string Environment,
    bool TelemetryEnabled,
    bool MetricsEnabled,
    bool TracingEnabled,
    string Exporter);
