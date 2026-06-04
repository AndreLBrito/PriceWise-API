using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PriceWise.Api.Telemetry;
using PriceWise.Application.Abstractions.Telemetry;

namespace PriceWise.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddPriceWiseOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = ReadOptions(configuration);

        services.Configure<TelemetryOptions>(telemetryOptions =>
        {
            telemetryOptions.Enabled = options.Enabled;
            telemetryOptions.ServiceName = options.ServiceName;
            telemetryOptions.ServiceVersion = options.ServiceVersion;
            telemetryOptions.Exporter = options.Exporter;
            telemetryOptions.OtlpEndpoint = options.OtlpEndpoint;
            telemetryOptions.EnableMetrics = options.EnableMetrics;
            telemetryOptions.EnableTracing = options.EnableTracing;
        });

        services.AddHealthChecks()
            .AddCheck<TelemetryHealthCheck>("telemetry");

        if (!options.Enabled)
        {
            return services;
        }

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment.EnvironmentName
            });

        var openTelemetryBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion));

        if (options.EnableTracing)
        {
            openTelemetryBuilder.WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(TelemetryConstants.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                AddTraceExporter(tracing, options);
            });
        }

        if (options.EnableMetrics)
        {
            openTelemetryBuilder.WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(TelemetryConstants.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                AddMetricExporter(metrics, options);
            });
        }

        return services;
    }

    private static void AddTraceExporter(TracerProviderBuilder builder, TelemetryOptions options)
    {
        if (IsOtlp(options))
        {
            builder.AddOtlpExporter(exporter => ConfigureOtlpExporter(exporter, options));
            return;
        }

        builder.AddConsoleExporter();
    }

    private static void AddMetricExporter(MeterProviderBuilder builder, TelemetryOptions options)
    {
        if (IsOtlp(options))
        {
            builder.AddOtlpExporter(exporter => ConfigureOtlpExporter(exporter, options));
            return;
        }

        builder.AddConsoleExporter();
    }

    private static bool IsOtlp(TelemetryOptions options)
    {
        return string.Equals(options.Exporter, "OTLP", StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.Exporter, "Otlp", StringComparison.OrdinalIgnoreCase);
    }

    private static TelemetryOptions ReadOptions(IConfiguration configuration)
    {
        return new TelemetryOptions
        {
            Enabled = ReadBool(configuration, $"{TelemetryOptions.SectionName}:Enabled", true),
            ServiceName = configuration[$"{TelemetryOptions.SectionName}:ServiceName"] ?? "PriceWise.Api",
            ServiceVersion = configuration[$"{TelemetryOptions.SectionName}:ServiceVersion"] ?? "1.0.0",
            Exporter = configuration[$"{TelemetryOptions.SectionName}:Exporter"] ?? "Console",
            OtlpEndpoint = configuration[$"{TelemetryOptions.SectionName}:OtlpEndpoint"],
            EnableMetrics = ReadBool(configuration, $"{TelemetryOptions.SectionName}:EnableMetrics", true),
            EnableTracing = ReadBool(configuration, $"{TelemetryOptions.SectionName}:EnableTracing", true)
        };
    }

    private static void ConfigureOtlpExporter(OtlpExporterOptions exporter, TelemetryOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            exporter.Endpoint = new Uri(options.OtlpEndpoint);
        }
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        return bool.TryParse(configuration[key], out var value) ? value : defaultValue;
    }
}
