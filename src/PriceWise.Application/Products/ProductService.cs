using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Common;
using PriceWise.Application.Products.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository productRepository;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;
    private readonly IApplicationTelemetry telemetry;

    public ProductService(
        IProductRepository productRepository,
        IDashboardCacheInvalidator dashboardCacheInvalidator,
        IApplicationTelemetry telemetry)
    {
        this.productRepository = productRepository;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
        this.telemetry = telemetry;
    }

    public async Task<Result<ProductResponse>> CreateAsync(
        Guid userId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("ProductService.Create");
        var productUrl = NormalizeUrl(request.ProductUrl);
        var existingProduct = await productRepository.GetByProductUrlAsync(
            userId,
            productUrl,
            cancellationToken);

        if (existingProduct is not null)
        {
            telemetry.RecordError(ProductErrors.ProductUrlAlreadyRegistered.Code);
            return Result<ProductResponse>.Failure(ProductErrors.ProductUrlAlreadyRegistered);
        }

        var product = Product.Create(
            userId,
            request.Name,
            request.Description,
            request.Brand,
            request.Category,
            productUrl,
            request.ImageUrl);

        await productRepository.AddAsync(product, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, product.Id, cancellationToken);
        telemetry.RecordProductCreated();

        return Result<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<Result<PagedResponse<ProductResponse>>> ListAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("ProductService.List");
        var products = await productRepository.ListByUserIdAsync(userId, request, cancellationToken);
        var response = PagedResponse<ProductResponse>.Create(
            products.Items
            .Select(MapToResponse)
            .ToArray(),
            products.Page,
            products.PageSize,
            products.TotalItems);

        return Result<PagedResponse<ProductResponse>>.Success(response);
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("ProductService.GetById");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(ProductErrors.ProductNotFound.Code);
            return Result<ProductResponse>.Failure(ProductErrors.ProductNotFound);
        }

        return Result<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<Result<ProductResponse>> UpdateAsync(
        Guid userId,
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("ProductService.Update");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(ProductErrors.ProductNotFound.Code);
            return Result<ProductResponse>.Failure(ProductErrors.ProductNotFound);
        }

        var productUrl = NormalizeUrl(request.ProductUrl);
        var existingProduct = await productRepository.GetByProductUrlAsync(
            userId,
            productUrl,
            cancellationToken);

        if (existingProduct is not null && existingProduct.Id != product.Id)
        {
            telemetry.RecordError(ProductErrors.ProductUrlAlreadyRegistered.Code);
            return Result<ProductResponse>.Failure(ProductErrors.ProductUrlAlreadyRegistered);
        }

        product.Update(
            request.Name,
            request.Description,
            request.Brand,
            request.Category,
            productUrl,
            request.ImageUrl);

        await productRepository.UpdateAsync(product, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, product.Id, cancellationToken);

        return Result<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("ProductService.Delete");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(ProductErrors.ProductNotFound.Code);
            return Result.Failure(ProductErrors.ProductNotFound);
        }

        product.Deactivate();
        await productRepository.UpdateAsync(product, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, product.Id, cancellationToken);

        return Result.Success();
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Brand,
            product.Category,
            product.ProductUrl,
            product.ImageUrl,
            product.IsActive,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }

    private static string NormalizeUrl(string productUrl)
    {
        return productUrl.Trim();
    }
}
