using FluentValidation;
using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Common;
using PriceWise.Application.PriceHistories;
using PriceWise.Application.PriceHistories.Dtos;

namespace PriceWise.Api.Features.PriceHistories;

public static class PriceHistoryEndpoints
{
    public static IEndpointRouteBuilder MapPriceHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/price-histories", CreateAsync)
            .WithTags("Histórico de preços")
            .WithName("CreatePriceHistory")
            .WithSummary("Registra um histórico de preço")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser)
            .RequireRateLimiting(RateLimitPolicyNames.General);

        var group = app.MapGroup("/api/products/{productId:guid}/price-histories")
            .WithTags("Histórico de preços")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser);

        group.MapGet("/", ListByProductAsync)
            .WithName("ListPriceHistoriesByProduct")
            .WithSummary("Lista o histórico de preços de um produto");

        group.MapGet("/latest", GetLatestAsync)
            .WithName("GetLatestPriceHistory")
            .WithSummary("Retorna o preço mais recente de um produto");

        group.MapGet("/lowest", GetLowestAsync)
            .WithName("GetLowestPriceHistory")
            .WithSummary("Retorna o menor preço de um produto");

        group.MapGet("/average", GetAverageAsync)
            .WithName("GetAveragePriceHistory")
            .WithSummary("Retorna o preço médio de um produto");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreatePriceHistoryRequest request,
        HttpContext httpContext,
        IValidator<CreatePriceHistoryRequest> validator,
        IPriceHistoryService priceHistoryService,
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

        var result = await priceHistoryService.CreateAsync(userId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/products/{result.Value.ProductId}/price-histories", ApiResponse<PriceHistoryResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> ListByProductAsync(
        Guid productId,
        HttpContext httpContext,
        IPriceHistoryService priceHistoryService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceHistoryService.ListByProductAsync(userId, productId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<IReadOnlyCollection<PriceHistoryResponse>>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetLatestAsync(
        Guid productId,
        HttpContext httpContext,
        IPriceHistoryService priceHistoryService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceHistoryService.GetLatestAsync(userId, productId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<PriceHistoryResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetLowestAsync(
        Guid productId,
        HttpContext httpContext,
        IPriceHistoryService priceHistoryService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceHistoryService.GetLowestAsync(userId, productId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<PriceHistoryResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> GetAverageAsync(
        Guid productId,
        HttpContext httpContext,
        IPriceHistoryService priceHistoryService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await priceHistoryService.GetAverageAsync(userId, productId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<AveragePriceHistoryResponse>.Ok(result.Value))
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
            "PriceHistories.ProductNotFound" => Results.NotFound(response),
            "PriceHistories.StoreNotFound" => Results.NotFound(response),
            "PriceHistories.PriceHistoryNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
