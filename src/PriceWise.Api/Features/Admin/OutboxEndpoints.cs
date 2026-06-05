using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Common;
using PriceWise.Application.Outbox;
using PriceWise.Application.Outbox.Dtos;

namespace PriceWise.Api.Features.Admin;

public static class OutboxEndpoints
{
    public static IEndpointRouteBuilder MapOutboxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/outbox")
            .WithTags("Outbox")
            .RequireAuthorization(AuthorizationPolicyNames.AdminOnly)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapGet("/", ListAsync)
            .WithName("ListOutboxMessages")
            .WithSummary("Lista mensagens da outbox");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetOutboxMessage")
            .WithSummary("Retorna uma mensagem da outbox pelo id");

        group.MapPost("/{id:guid}/retry", RetryAsync)
            .WithName("RetryOutboxMessage")
            .WithSummary("Reenfileira uma mensagem da outbox com falha");

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] OutboxListRequest request,
        IOutboxService outboxService,
        CancellationToken cancellationToken)
    {
        var result = await outboxService.ListAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<PagedResponse<OutboxMessageResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IOutboxService outboxService,
        CancellationToken cancellationToken)
    {
        var result = await outboxService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<OutboxMessageResponse>.Ok(result.Value))
            : Results.NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));
    }

    private static async Task<IResult> RetryAsync(
        Guid id,
        IOutboxService outboxService,
        CancellationToken cancellationToken)
    {
        var result = await outboxService.RetryAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ApiResponse<OutboxMessageResponse>.Ok(result.Value));
        }

        return result.Error == OutboxErrors.MessageNotFound
            ? Results.NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message))
            : Results.Conflict(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));
    }
}
