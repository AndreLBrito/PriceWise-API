using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Application.Common;
using PriceWise.Application.PriceChecks;
using PriceWise.Application.PriceChecks.Dtos;

namespace PriceWise.Api.Features.PriceChecks;

public static class PriceCheckEndpoints
{
    public static IEndpointRouteBuilder MapPriceCheckEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/price-check")
            .WithTags("Verificação de preços")
            .RequireAuthorization();

        // TODO: Restringir estes endpoints a usuários administradores quando o modelo de autorização existir.
        group.MapPost("/run", RunAsync)
            .WithName("RunPriceCheck")
            .WithSummary("Executa manualmente a verificação simulada de preços");

        group.MapGet("/status", GetStatusAsync)
            .WithName("GetPriceCheckStatus")
            .WithSummary("Retorna o status da verificação automática de preços");

        return app;
    }

    private static async Task<IResult> RunAsync(
        HttpContext httpContext,
        IPriceCheckService priceCheckService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out _))
        {
            return Unauthorized();
        }

        var result = await priceCheckService.RunAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<PriceCheckRunResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetStatusAsync(
        HttpContext httpContext,
        IPriceCheckService priceCheckService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out _))
        {
            return Unauthorized();
        }

        var result = await priceCheckService.GetStatusAsync(cancellationToken);

        return Results.Ok(ApiResponse<PriceCheckStatusResponse>.Ok(result.Value));
    }

    private static IResult Unauthorized()
    {
        var response = ApiResponse<object>.Fail(
            "Auth.Unauthorized",
            "Usuário não autenticado.");

        return Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult Failure(Error error)
    {
        var response = ApiResponse<object>.Fail(error.Code, error.Message);

        return Results.BadRequest(response);
    }
}
