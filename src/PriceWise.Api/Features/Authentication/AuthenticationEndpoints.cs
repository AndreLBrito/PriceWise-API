using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Auditing;
using PriceWise.Application.Authentication;
using PriceWise.Application.Authentication.Dtos;
using PriceWise.Application.Common;

namespace PriceWise.Api.Features.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Autenticação");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Cadastra um novo usuário")
            .RequireRateLimiting(RateLimitPolicyNames.Login);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Autentica um usuário")
            .RequireRateLimiting(RateLimitPolicyNames.Login);

        group.MapPost("/refresh-token", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithSummary("Renova o access token e o refresh token")
            .RequireRateLimiting(RateLimitPolicyNames.RefreshToken);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Revoga um refresh token");

        group.MapGet("/me", GetMeAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Retorna os dados seguros do usuário autenticado")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapPost("/change-password", ChangePasswordAsync)
            .WithName("ChangePassword")
            .WithSummary("Altera a senha do usuário autenticado")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapPost("/revoke-refresh-tokens", RevokeRefreshTokensAsync)
            .WithName("RevokeOwnRefreshTokens")
            .WithSummary("Revoga todos os refresh tokens ativos do usuário autenticado")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IAuthService authService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.RegisterAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            result.Value.UserId,
            AuditActions.Create,
            "User",
            result.Value.UserId,
            NewValues: new
            {
                result.Value.UserId,
                result.Value.Name,
                result.Value.Email,
                result.Value.Role
            }), cancellationToken);

        return Results.Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthService authService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.LoginAsync(request, cancellationToken);

        await auditLogService.RecordAsync(new AuditLogEntry(
            result.IsSuccess ? result.Value.UserId : null,
            AuditActions.Login,
            "User",
            result.IsSuccess ? result.Value.UserId : null,
            NewValues: new
            {
                request.Email,
                Status = result.IsSuccess ? "Success" : "Failed",
                ErrorCode = result.IsFailure ? result.Error.Code : null
            }), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AuthResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.RefreshTokenAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AuthResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        IValidator<LogoutRequest> validator,
        IAuthService authService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.LogoutAsync(request, cancellationToken);

        await auditLogService.RecordAsync(new AuditLogEntry(
            null,
            AuditActions.Logout,
            "RefreshToken",
            null,
            NewValues: new { Status = result.IsSuccess ? "Success" : "Failed" }), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<object>.Ok(new { }))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetMeAsync(
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await authService.GetMeAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<CurrentUserResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> ChangePasswordAsync(
        HttpContext httpContext,
        ChangePasswordRequest request,
        IValidator<ChangePasswordRequest> validator,
        IAuthService authService,
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

        var result = await authService.ChangePasswordAsync(userId, request, cancellationToken);

        if (result.IsSuccess)
        {
            await auditLogService.RecordAsync(new AuditLogEntry(
                userId,
                AuditActions.ChangePassword,
                "User",
                userId,
                NewValues: new { Status = "Success" }), cancellationToken);
        }

        return result.IsSuccess
            ? Results.Ok(ApiResponse<object>.Ok(new { Message = "Senha alterada com sucesso." }))
            : Failure(result.Error);
    }

    private static async Task<IResult> RevokeRefreshTokensAsync(
        HttpContext httpContext,
        IAuthService authService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await authService.RevokeRefreshTokensAsync(userId, cancellationToken);

        if (result.IsSuccess)
        {
            await auditLogService.RecordAsync(new AuditLogEntry(
                userId,
                AuditActions.RevokeRefreshTokens,
                "User",
                userId,
                NewValues: new { Status = "Success" }), cancellationToken);
        }

        return result.IsSuccess
            ? Results.Ok(ApiResponse<object>.Ok(new { Message = "Refresh tokens revogados com sucesso." }))
            : Failure(result.Error);
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
            "Auth.EmailAlreadyRegistered" => Results.Conflict(response),
            "Auth.InvalidCredentials" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            "Auth.InvalidRefreshToken" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            "Auth.UserInactive" => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
            "Auth.UserLocked" => Results.Json(response, statusCode: StatusCodes.Status423Locked),
            "Auth.UserNotFound" => Results.NotFound(response),
            "Auth.InvalidCurrentPassword" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.BadRequest(response)
        };
    }
}
