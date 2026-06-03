using System.Reflection;
using PriceWise.Api.Common;

namespace PriceWise.Api.Features.System;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/system")
            .WithTags("Sistema");

        group.MapGet("/info", (IHostEnvironment environment) =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                var response = new SystemInfoResponse(
                    assembly.GetName().Name ?? "PriceWise.Api",
                    assembly.GetName().Version?.ToString() ?? "1.0.0",
                    environment.EnvironmentName,
                    DateTime.UtcNow);

                return Results.Ok(ApiResponse<SystemInfoResponse>.Ok(response));
            })
            .WithName("GetSystemInfo")
            .WithSummary("Retorna informações do sistema");

        return app;
    }
}
