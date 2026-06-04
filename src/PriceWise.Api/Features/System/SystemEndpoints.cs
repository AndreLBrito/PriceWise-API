using System.Reflection;
using PriceWise.Api.Common;
using PriceWise.Application.Abstractions.DataSeeding;

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

        group.MapPost("/seed-demo-data", SeedDemoDataAsync)
            .RequireAuthorization()
            .WithName("SeedDemoData")
            .WithSummary("Executa o seed de dados de demonstração");

        return app;
    }

    private static async Task<IResult> SeedDemoDataAsync(
        IDataSeeder dataSeeder,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        // TODO: restringir para usuário administrador quando o conceito de roles existir.
        if (environment.IsProduction())
        {
            return Results.BadRequest(ApiResponse<object>.Fail(
                "DataSeed.Production",
                "Seed de demonstração não pode ser executado em Production."));
        }

        var result = await dataSeeder.SeedAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<object>.Ok(new { Message = "Seed de demonstração executado com sucesso." }))
            : Results.BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));
    }
}
