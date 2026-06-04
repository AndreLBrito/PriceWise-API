using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Application.Common;
using PriceWise.Application.Dashboard;
using PriceWise.Application.Dashboard.Dtos;

namespace PriceWise.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetDashboardSummary")
            .WithSummary("Retorna o resumo geral do usuário autenticado");

        group.MapGet("/products/{productId:guid}/summary", GetProductPriceSummaryAsync)
            .WithName("GetDashboardProductPriceSummary")
            .WithSummary("Retorna o resumo de preços de um produto monitorado");

        group.MapGet("/stores/{storeId:guid}/summary", GetStorePriceSummaryAsync)
            .WithName("GetDashboardStorePriceSummary")
            .WithSummary("Retorna o resumo de preços de uma loja monitorada");

        group.MapGet("/alerts/summary", GetAlertSummaryAsync)
            .WithName("GetDashboardAlertSummary")
            .WithSummary("Retorna o resumo de alertas do usuário autenticado");

        return app;
    }

    private static async Task<IResult> GetSummaryAsync(
        HttpContext httpContext,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await dashboardService.GetSummaryAsync(userId, cancellationToken);

        return Results.Ok(ApiResponse<DashboardSummaryResponse>.Ok(result.Value));
    }

    private static async Task<IResult> GetProductPriceSummaryAsync(
        Guid productId,
        HttpContext httpContext,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await dashboardService.GetProductPriceSummaryAsync(userId, productId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<ProductPriceSummaryResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetStorePriceSummaryAsync(
        Guid storeId,
        HttpContext httpContext,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await dashboardService.GetStorePriceSummaryAsync(userId, storeId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<StorePriceSummaryResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetAlertSummaryAsync(
        HttpContext httpContext,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await dashboardService.GetAlertSummaryAsync(userId, cancellationToken);

        return Results.Ok(ApiResponse<AlertSummaryResponse>.Ok(result.Value));
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

        return error.Code switch
        {
            "Dashboard.ProductNotFound" => Results.NotFound(response),
            "Dashboard.StoreNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
