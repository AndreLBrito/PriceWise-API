using Microsoft.Extensions.Diagnostics.HealthChecks;
using PriceWise.Api.Common;

namespace PriceWise.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
            .WithTags("Saúde");

        group.MapGet("/", () => Results.Ok(ApiResponse<HealthResponse>.Ok(new HealthResponse("Saudável"))))
            .WithName("GetHealth")
            .WithSummary("Retorna o status de saúde da API");

        group.MapGet("/telemetry", async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
            {
                var report = await healthCheckService.CheckHealthAsync(
                    check => check.Name == "telemetry",
                    cancellationToken);
                var status = report.Status == HealthStatus.Healthy ? "Saudável" : "Indisponível";

                return Results.Ok(ApiResponse<HealthResponse>.Ok(new HealthResponse(status)));
            })
            .WithName("GetTelemetryHealth")
            .WithSummary("Retorna o status de saúde da telemetria");

        return app;
    }
}
