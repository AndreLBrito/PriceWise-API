using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Auditing;
using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;

namespace PriceWise.Api.Features.Admin;

public static class AuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/audit-logs")
            .WithTags("Auditoria")
            .RequireAuthorization(AuthorizationPolicyNames.AdminOnly)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapGet("/", ListAsync)
            .WithName("ListAuditLogs")
            .WithSummary("Lista registros de auditoria");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAuditLog")
            .WithSummary("Retorna um registro de auditoria pelo id");

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] AuditLogListRequest request,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var result = await auditLogService.ListAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<PagedResponse<AuditLogResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var result = await auditLogService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AuditLogResponse>.Ok(result.Value))
            : Results.NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));
    }
}
