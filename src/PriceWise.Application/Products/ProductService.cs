using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.Products.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository productRepository;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;

    public ProductService(
        IProductRepository productRepository,
        IDashboardCacheInvalidator dashboardCacheInvalidator)
    {
        this.productRepository = productRepository;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
    }

    public async Task<Result<ProductResponse>> CreateAsync(
        Guid userId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var productUrl = NormalizeUrl(request.ProductUrl);
        var existingProduct = await productRepository.GetByProductUrlAsync(
            userId,
            productUrl,
            cancellationToken);

        if (existingProduct is not null)
        {
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

        return Result<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<Result<IReadOnlyCollection<ProductResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var products = await productRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = products
            .Select(MapToResponse)
            .ToArray();

        return Result<IReadOnlyCollection<ProductResponse>>.Success(response);
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        return product is null
            ? Result<ProductResponse>.Failure(ProductErrors.ProductNotFound)
            : Result<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<Result<ProductResponse>> UpdateAsync(
        Guid userId,
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            return Result<ProductResponse>.Failure(ProductErrors.ProductNotFound);
        }

        var productUrl = NormalizeUrl(request.ProductUrl);
        var existingProduct = await productRepository.GetByProductUrlAsync(
            userId,
            productUrl,
            cancellationToken);

        if (existingProduct is not null && existingProduct.Id != product.Id)
        {
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
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
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
