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

        return app;
    }
}
