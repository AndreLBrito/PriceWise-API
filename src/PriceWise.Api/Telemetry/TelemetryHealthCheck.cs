using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace PriceWise.Api.Telemetry;

public sealed class TelemetryHealthCheck : IHealthCheck
{
    private readonly TelemetryOptions options;

    public TelemetryHealthCheck(IOptions<TelemetryOptions> options)
    {
        this.options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["enabled"] = options.Enabled,
            ["metricsEnabled"] = options.EnableMetrics,
            ["tracingEnabled"] = options.EnableTracing,
            ["exporter"] = options.Exporter
        };

        return Task.FromResult(HealthCheckResult.Healthy("Telemetria configurada.", data));
    }
}
