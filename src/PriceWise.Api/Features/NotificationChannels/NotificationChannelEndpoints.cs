using PriceWise.Api.Authorization;
using FluentValidation;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Auditing;
using PriceWise.Application.Common;
using PriceWise.Application.NotificationChannels;
using PriceWise.Application.NotificationChannels.Dtos;

namespace PriceWise.Api.Features.NotificationChannels;

public static class NotificationChannelEndpoints
{
    public static IEndpointRouteBuilder MapNotificationChannelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notification-channels")
            .WithTags("Canais de notificação")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapPost("/", CreateAsync)
            .WithName("CreateNotificationChannel")
            .WithSummary("Cadastra um canal de notificação");

        group.MapGet("/", ListAsync)
            .WithName("ListNotificationChannels")
            .WithSummary("Lista canais de notificação do usuário autenticado");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetNotificationChannelById")
            .WithSummary("Retorna um canal de notificação do usuário autenticado");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateNotificationChannel")
            .WithSummary("Atualiza um canal de notificação");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteNotificationChannel")
            .WithSummary("Remove logicamente um canal de notificação");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateNotificationChannelRequest request,
        HttpContext httpContext,
        IValidator<CreateNotificationChannelRequest> validator,
        INotificationChannelService notificationChannelService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await notificationChannelService.CreateAsync(userId, request, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Create,
            "NotificationChannel",
            result.Value.Id,
            NewValues: result.Value), cancellationToken);

        return Results.Created(
            $"/api/notification-channels/{result.Value.Id}",
            ApiResponse<NotificationChannelResponse>.Ok(result.Value));
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        [AsParameters] ListRequest request,
        INotificationChannelService notificationChannelService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await notificationChannelService.ListAsync(userId, request, cancellationToken);

        return Results.Ok(ApiResponse<PagedResponse<NotificationChannelResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        HttpContext httpContext,
        INotificationChannelService notificationChannelService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await notificationChannelService.GetByIdAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<NotificationChannelResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateNotificationChannelRequest request,
        HttpContext httpContext,
        IValidator<UpdateNotificationChannelRequest> validator,
        INotificationChannelService notificationChannelService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var oldValues = await notificationChannelService.GetByIdAsync(userId, id, cancellationToken);
        var result = await notificationChannelService.UpdateAsync(userId, id, request, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Update,
            "NotificationChannel",
            id,
            oldValues.IsSuccess ? oldValues.Value : null,
            result.Value), cancellationToken);

        return Results.Ok(ApiResponse<NotificationChannelResponse>.Ok(result.Value));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        HttpContext httpContext,
        INotificationChannelService notificationChannelService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var oldValues = await notificationChannelService.GetByIdAsync(userId, id, cancellationToken);
        var result = await notificationChannelService.DeleteAsync(userId, id, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Delete,
            "NotificationChannel",
            id,
            oldValues.IsSuccess ? oldValues.Value : null,
            new { IsActive = false }), cancellationToken);

        return Results.NoContent();
    }

    private static IResult ValidationFailure(string message)
    {
        return Results.BadRequest(ApiResponse<object>.Fail("Validation.InvalidRequest", message));
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
            "NotificationChannels.DuplicateChannel" => Results.Conflict(response),
            "NotificationChannels.NotificationChannelNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
