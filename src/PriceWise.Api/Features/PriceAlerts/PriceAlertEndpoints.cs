using FluentValidation;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Common;
using PriceWise.Application.PriceAlerts;
using PriceWise.Application.PriceAlerts.Dtos;

namespace PriceWise.Api.Features.PriceAlerts;

public static class PriceAlertEndpoints
{
    public static IEndpointRouteBuilder MapPriceAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/price-alerts")
            .WithTags("Alertas de preço")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapPost("/", CreateAsync)
            .WithName("CreatePriceAlert")
            .WithSummary("Cadastra um alerta de preço");

        group.MapGet("/", ListAsync)
            .WithName("ListPriceAlerts")
            .WithSummary("Lista alertas de preço do usuário autenticado");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetPriceAlertById")
            .WithSummary("Retorna um alerta de preço do usuário autenticado");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdatePriceAlert")
            .WithSummary("Atualiza um alerta de preço");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeletePriceAlert")
            .WithSummary("Remove logicamente um alerta de preço");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreatePriceAlertRequest request,
        HttpContext httpContext,
        IValidator<CreatePriceAlertRequest> validator,
        IPriceAlertService priceAlertService,
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

        var result = await priceAlertService.CreateAsync(userId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/price-alerts/{result.Value.Id}", ApiResponse<PriceAlertResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        IPriceAlertService priceAlertService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceAlertService.ListAsync(userId, cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyCollection<PriceAlertResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        HttpContext httpContext,
        IPriceAlertService priceAlertService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceAlertService.GetByIdAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<PriceAlertResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdatePriceAlertRequest request,
        HttpContext httpContext,
        IValidator<UpdatePriceAlertRequest> validator,
        IPriceAlertService priceAlertService,
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

        var result = await priceAlertService.UpdateAsync(userId, id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<PriceAlertResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        HttpContext httpContext,
        IPriceAlertService priceAlertService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceAlertService.DeleteAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
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
            "PriceAlerts.ProductNotFound" => Results.NotFound(response),
            "PriceAlerts.ActiveAlertAlreadyExists" => Results.Conflict(response),
            "PriceAlerts.PriceAlertNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
