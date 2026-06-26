using FluentValidation;
using PriceWise.Api.Authorization;
using PriceWise.Api.Common;
using PriceWise.Api.Extensions;
using PriceWise.Api.RateLimiting;
using PriceWise.Application.Auditing;
using PriceWise.Application.Common;
using PriceWise.Application.Products;
using PriceWise.Application.Products.Dtos;

namespace PriceWise.Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Produtos")
            .RequireAuthorization(AuthorizationPolicyNames.AuthenticatedUser)
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

        var result = await productService.CreateAsync(userId, request, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Create,
            "Product",
            result.Value.Id,
            NewValues: result.Value), cancellationToken);

        return Results.Created($"/api/v1/products/{result.Value.Id}", ApiResponse<ProductResponse>.Ok(result.Value));
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        [AsParameters] ListRequest request,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await productService.ListAsync(userId, request, cancellationToken);

        return Results.Ok(ApiResponse<PagedResponse<ProductResponse>>.Ok(result.Value));
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

        var oldValues = await productService.GetByIdAsync(userId, id, cancellationToken);
        var result = await productService.UpdateAsync(userId, id, request, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Update,
            "Product",
            id,
            oldValues.IsSuccess ? oldValues.Value : null,
            result.Value), cancellationToken);

        return Results.Ok(ApiResponse<ProductResponse>.Ok(result.Value));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        HttpContext httpContext,
        IProductService productService,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var oldValues = await productService.GetByIdAsync(userId, id, cancellationToken);
        var result = await productService.DeleteAsync(userId, id, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Error);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Delete,
            "Product",
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
            "Products.ProductUrlAlreadyRegistered" => Results.Conflict(response),
            "Products.ProductNotFound" => Results.NotFound(response),
            _ => Results.BadRequest(response)
        };
    }
}
