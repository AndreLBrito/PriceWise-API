using FluentValidation;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Common;
using PriceWise.Application.Stores;
using PriceWise.Application.Stores.Dtos;

namespace PriceWise.Api.Features.Stores;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores")
            .WithTags("Lojas")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapPost("/", CreateAsync)
            .WithName("CreateStore")
            .WithSummary("Cadastra uma loja");

        group.MapGet("/", ListAsync)
            .WithName("ListStores")
            .WithSummary("Lista lojas do usuário autenticado");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetStoreById")
            .WithSummary("Retorna uma loja do usuário autenticado");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateStore")
            .WithSummary("Atualiza uma loja");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteStore")
            .WithSummary("Remove logicamente uma loja");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateStoreRequest request,
        HttpContext httpContext,
        IValidator<CreateStoreRequest> validator,
        IStoreService storeService,
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

        var result = await storeService.CreateAsync(userId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/stores/{result.Value.Id}", ApiResponse<StoreResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        IStoreService storeService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await storeService.ListAsync(userId, cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyCollection<StoreResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        HttpContext httpContext,
        IStoreService storeService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await storeService.GetByIdAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<StoreResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateStoreRequest request,
        HttpContext httpContext,
        IValidator<UpdateStoreRequest> validator,
        IStoreService storeService,
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

        var result = await storeService.UpdateAsync(userId, id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<StoreResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        HttpContext httpContext,
        IStoreService storeService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await storeService.DeleteAsync(userId, id, cancellationToken);

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
            "Stores.BaseUrlAlreadyRegistered" => Results.Conflict(response),
            "Stores.StoreNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
