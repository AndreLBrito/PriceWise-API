using Microsoft.Extensions.Options;
using PriceWise.Api.Common;
using PriceWise.Api.RateLimiting;
using PriceWise.Api.Telemetry;

namespace PriceWise.Api.Features.Telemetry;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telemetry")
            .WithTags("Telemetria")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapGet("/info", GetInfo)
            .WithName("GetTelemetryInfo")
            .WithSummary("Retorna informações de telemetria da aplicação");

        return app;
    }

    private static IResult GetInfo(
        IOptions<TelemetryOptions> options,
        IHostEnvironment environment)
    {
        var telemetryOptions = options.Value;
        var response = new TelemetryInfoResponse(
            telemetryOptions.ServiceName,
            telemetryOptions.ServiceVersion,
            environment.EnvironmentName,
            telemetryOptions.Enabled,
            telemetryOptions.EnableMetrics,
            telemetryOptions.EnableTracing,
            telemetryOptions.Exporter);

        return Results.Ok(ApiResponse<TelemetryInfoResponse>.Ok(response));
    }
}
