using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Auditing;
using PriceWise.Application.Common;
using PriceWise.Application.PriceChecks;
using PriceWise.Application.PriceChecks.Dtos;

namespace PriceWise.Api.Features.PriceChecks;

public static class PriceCheckEndpoints
{
    public static IEndpointRouteBuilder MapPriceCheckEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/price-check")
            .WithTags("Verificação de preços")
            .RequireAuthorization(AuthorizationPolicyNames.PriceCheckManagement);

        group.MapPost("/run", RunAsync)
            .WithName("RunPriceCheck")
            .WithSummary("Executa manualmente a verificação simulada de preços")
            .RequireRateLimiting(RateLimitPolicyNames.PriceCheck);

        group.MapGet("/status", GetStatusAsync)
            .WithName("GetPriceCheckStatus")
            .WithSummary("Retorna o status da verificação automática de preços")
            .RequireRateLimiting(RateLimitPolicyNames.General);

        return app;
    }

    private static async Task<IResult> RunAsync(
        HttpContext httpContext,
        IPriceCheckService priceCheckService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out _))
        {
            return Unauthorized();
        }

        var result = await priceCheckService.RunAsync(cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            httpContext.User.TryGetUserId(out var userId) ? userId : null,
            AuditActions.ManualPriceCheck,
            "PriceCheck",
            null,
            NewValues: result.Value), cancellationToken);

        return Results.Ok(ApiResponse<PriceCheckRunResponse>.Ok(result.Value));
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
