using FluentValidation;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Common;
using PriceWise.Application.Products;
using PriceWise.Application.Products.Dtos;

namespace PriceWise.Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Produtos")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.General);

        group.MapPost("/", CreateAsync)
            .WithName("CreateProduct")
            .WithSummary("Cadastra um produto monitorado");

        group.MapGet("/", ListAsync)
            .WithName("ListProducts")
            .WithSummary("Lista produtos monitorados do usuário autenticado");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetProductById")
            .WithSummary("Retorna um produto monitorado do usuário autenticado");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateProduct")
            .WithSummary("Atualiza um produto monitorado");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteProduct")
            .WithSummary("Remove logicamente um produto monitorado");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        HttpContext httpContext,
        IValidator<CreateProductRequest> validator,
        IProductService productService,
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

        var result = await productService.CreateAsync(userId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/products/{result.Value.Id}", ApiResponse<ProductResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await productService.ListAsync(userId, cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyCollection<ProductResponse>>.Ok(result.Value));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        HttpContext httpContext,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await productService.GetByIdAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<ProductResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        HttpContext httpContext,
        IValidator<UpdateProductRequest> validator,
        IProductService productService,
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

        var result = await productService.UpdateAsync(userId, id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<ProductResponse>.Ok(result.Value))
            : Failure(result.Error);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        HttpContext httpContext,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await productService.DeleteAsync(userId, id, cancellationToken);

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
            "Products.ProductUrlAlreadyRegistered" => Results.Conflict(response),
            "Products.ProductNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
