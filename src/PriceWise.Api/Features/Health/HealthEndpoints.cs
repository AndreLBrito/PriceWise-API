using PriceWise.Api.Common;

namespace PriceWise.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
            .WithTags("Health");

        group.MapGet("/", () => Results.Ok(ApiResponse<HealthResponse>.Ok(new HealthResponse("Healthy"))))
            .WithName("GetHealth")
            .WithSummary("Returns the API health status");

        return app;
    }
}
