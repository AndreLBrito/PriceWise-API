using FluentValidation;
using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Auditing;
using PriceWise.Application.Admin;
using PriceWise.Application.Admin.Dtos;
using PriceWise.Application.Common;

namespace PriceWise.Api.Features.Admin;

public static class AdminUserEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Administração")
            .RequireAuthorization(AuthorizationPolicyNames.AdminOnly)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapGet("/", ListAsync)
            .WithName("ListAdminUsers")
            .WithSummary("Lista usuários com paginação simples");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAdminUser")
            .WithSummary("Retorna um usuário pelo id");

        group.MapPut("/{id:guid}/role", UpdateRoleAsync)
            .WithName("UpdateAdminUserRole")
            .WithSummary("Altera o papel de um usuário");

        group.MapPut("/{id:guid}/activate", ActivateAsync)
            .WithName("ActivateAdminUser")
            .WithSummary("Ativa um usuário");

        group.MapPut("/{id:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateAdminUser")
            .WithSummary("Desativa um usuário");

        group.MapPost("/{id:guid}/revoke-refresh-tokens", RevokeRefreshTokensAsync)
            .WithName("RevokeAdminUserRefreshTokens")
            .WithSummary("Revoga todos os refresh tokens ativos de um usuário");

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListRequest request,
        IAdminUserService adminUserService,
        CancellationToken cancellationToken)
    {
        var result = await adminUserService.ListAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<PagedResponse<AdminUserResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IAdminUserService adminUserService,
        CancellationToken cancellationToken)
    {
        var result = await adminUserService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AdminUserResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid id,
        UpdateUserRoleRequest request,
        HttpContext httpContext,
        IValidator<UpdateUserRoleRequest> validator,
        IAdminUserService adminUserService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var currentAdminUserId))
        {
            return Unauthorized();
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(
                "Validation.InvalidRequest",
                validationResult.Errors[0].ErrorMessage));
        }

        var oldValues = await adminUserService.GetByIdAsync(id, cancellationToken);
        var result = await adminUserService.UpdateRoleAsync(
            currentAdminUserId,
            id,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            currentAdminUserId,
            AuditActions.ChangeRole,
            "User",
            id,
            oldValues.IsSuccess ? oldValues.Value : null,
            result.Value), cancellationToken);

        return Results.Ok(ApiResponse<AdminUserResponse>.Ok(result.Value));
    }

    private static async Task<IResult> ActivateAsync(
        Guid id,
        HttpContext httpContext,
        IAdminUserService adminUserService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var currentAdminUserId))
        {
            return Unauthorized();
        }

        var oldValues = await adminUserService.GetByIdAsync(id, cancellationToken);
        var result = await adminUserService.ActivateAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            currentAdminUserId,
            AuditActions.Activate,
            "User",
            id,
            oldValues.IsSuccess ? oldValues.Value : null,
            result.Value), cancellationToken);

        return Results.Ok(ApiResponse<AdminUserResponse>.Ok(result.Value));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid id,
        HttpContext httpContext,
        IAdminUserService adminUserService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var currentAdminUserId))
        {
            return Unauthorized();
        }

        var oldValues = await adminUserService.GetByIdAsync(id, cancellationToken);
        var result = await adminUserService.DeactivateAsync(currentAdminUserId, id, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            currentAdminUserId,
            AuditActions.Deactivate,
            "User",
            id,
            oldValues.IsSuccess ? oldValues.Value : null,
            result.Value), cancellationToken);

        return Results.Ok(ApiResponse<AdminUserResponse>.Ok(result.Value));
    }

    private static async Task<IResult> RevokeRefreshTokensAsync(
        Guid id,
        HttpContext httpContext,
        IAdminUserService adminUserService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var currentAdminUserId))
        {
            return Unauthorized();
        }

        var result = await adminUserService.RevokeRefreshTokensAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            await auditLogService.RecordAsync(new AuditLogEntry(
                currentAdminUserId,
                AuditActions.RevokeRefreshTokens,
                "User",
                id,
                NewValues: new { Status = "Success" }), cancellationToken);
        }

        return result.IsSuccess
            ? Results.Ok(ApiResponse<object>.Ok(new { Message = "Refresh tokens revogados com sucesso." }))
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
            "Admin.UserNotFound" => Results.NotFound(response),
            "Admin.CannotDeactivateSelf" => Results.BadRequest(response),
            "Admin.CannotRemoveOwnAdminRole" => Results.BadRequest(response),
            _ => Results.BadRequest(response)
        };
    }
}
