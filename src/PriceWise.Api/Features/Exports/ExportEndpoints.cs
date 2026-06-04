using System.Text;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Common;
using PriceWise.Application.Exports;
using PriceWise.Application.Exports.Dtos;

namespace PriceWise.Api.Features.Exports;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exports")
            .WithTags("Exportações")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapGet("/products.csv", ExportProductsAsync)
            .WithName("ExportProductsCsv")
            .WithSummary("Exporta produtos em CSV");

        group.MapGet("/stores.csv", ExportStoresAsync)
            .WithName("ExportStoresCsv")
            .WithSummary("Exporta lojas em CSV");

        group.MapGet("/price-histories.csv", ExportPriceHistoriesAsync)
            .WithName("ExportPriceHistoriesCsv")
            .WithSummary("Exporta histórico de preços em CSV");

        group.MapGet("/alert-notifications.csv", ExportAlertNotificationsAsync)
            .WithName("ExportAlertNotificationsCsv")
            .WithSummary("Exporta notificações de alerta em CSV");

        return app;
    }

    private static async Task<IResult> ExportProductsAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? productId,
        HttpContext httpContext,
        IExportService exportService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await exportService.ExportProductsAsync(
            userId,
            new ExportFilter(startDate, endDate, productId),
            cancellationToken);

        return ToFileResult(result);
    }

    private static async Task<IResult> ExportStoresAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? storeId,
        HttpContext httpContext,
        IExportService exportService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await exportService.ExportStoresAsync(
            userId,
            new ExportFilter(startDate, endDate, StoreId: storeId),
            cancellationToken);

        return ToFileResult(result);
    }

    private static async Task<IResult> ExportPriceHistoriesAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? productId,
        Guid? storeId,
        HttpContext httpContext,
        IExportService exportService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await exportService.ExportPriceHistoriesAsync(
            userId,
            new ExportFilter(startDate, endDate, productId, storeId),
            cancellationToken);

        return ToFileResult(result);
    }

    private static async Task<IResult> ExportAlertNotificationsAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? productId,
        Guid? storeId,
        HttpContext httpContext,
        IExportService exportService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await exportService.ExportAlertNotificationsAsync(
            userId,
            new ExportFilter(startDate, endDate, productId, storeId),
            cancellationToken);

        return ToFileResult(result);
    }

    private static IResult ToFileResult(Result<CsvExportResponse> result)
    {
        if (result.IsFailure)
        {
            var response = ApiResponse<object>.Fail(result.Error.Code, result.Error.Message);
            return Results.BadRequest(response);
        }

        return Results.File(
            Encoding.UTF8.GetBytes(result.Value.Content),
            result.Value.ContentType,
            result.Value.FileName);
    }

    private static IResult Unauthorized()
    {
        var response = ApiResponse<object>.Fail(
            "Auth.Unauthorized",
            "Usuário não autenticado.");

        return Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
    }
}
