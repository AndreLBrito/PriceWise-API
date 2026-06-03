using FluentValidation;
using PriceWise.Api.Common;
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
            .WithSummary("Cadastra um novo usuário");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Autentica um usuário");

        group.MapPost("/refresh-token", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithSummary("Renova o access token e o refresh token");

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Revoga um refresh token");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.RegisterAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AuthResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.LoginAsync(request, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult.Errors[0].ErrorMessage);
        }

        var result = await authService.LogoutAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<object>.Ok(new { }))
            : Failure(result.Error);
    }

    private static IResult ValidationFailure(string message)
    {
        return Results.BadRequest(ApiResponse<object>.Fail("Validation.InvalidRequest", message));
    }

    private static IResult Failure(Error error)
    {
        var response = ApiResponse<object>.Fail(error.Code, error.Message);

        return error.Code switch
        {
            "Auth.EmailAlreadyRegistered" => Results.Conflict(response),
            "Auth.InvalidCredentials" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            "Auth.InvalidRefreshToken" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.BadRequest(response)
        };
    }
}
