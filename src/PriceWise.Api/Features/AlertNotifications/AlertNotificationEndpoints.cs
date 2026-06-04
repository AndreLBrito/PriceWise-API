using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Application.AlertNotifications;
using PriceWise.Application.AlertNotifications.Dtos;
using PriceWise.Application.Common;

namespace PriceWise.Api.Features.AlertNotifications;

public static class AlertNotificationEndpoints
{
    public static IEndpointRouteBuilder MapAlertNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alert-notifications")
            .WithTags("Notificações de alerta")
            .RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithName("ListAlertNotifications")
            .WithSummary("Lista notificações de alerta do usuário autenticado");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAlertNotificationById")
            .WithSummary("Retorna uma notificação de alerta do usuário autenticado");

        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        IAlertNotificationService alertNotificationService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await alertNotificationService.ListAsync(userId, cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyCollection<AlertNotificationResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        HttpContext httpContext,
        IAlertNotificationService alertNotificationService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await alertNotificationService.GetByIdAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AlertNotificationResponse>.Ok(result.Value))
            : Failure(result.Error);
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
            "AlertNotifications.AlertNotificationNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
