using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.Products.Dtos;

namespace PriceWise.Application.Products;

public interface IProductService : IService
{
    Task<Result<ProductResponse>> CreateAsync(
        Guid userId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<ProductResponse>>> ListAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProductResponse>> GetByIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Result<ProductResponse>> UpdateAsync(
        Guid userId,
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);
}
