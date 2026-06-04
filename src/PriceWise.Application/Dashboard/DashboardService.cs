using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.Dashboard.Dtos;

namespace PriceWise.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository dashboardRepository;
    private readonly IProductRepository productRepository;
    private readonly IStoreRepository storeRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository)
    {
        this.dashboardRepository = dashboardRepository;
        this.productRepository = productRepository;
        this.storeRepository = storeRepository;
    }

    public async Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var summary = await dashboardRepository.GetSummaryAsync(userId, cancellationToken);

        return Result<DashboardSummaryResponse>.Success(summary);
    }

    public async Task<Result<ProductPriceSummaryResponse>> GetProductPriceSummaryAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            return Result<ProductPriceSummaryResponse>.Failure(DashboardErrors.ProductNotFound);
        }

        var summary = await dashboardRepository.GetProductPriceSummaryAsync(
            userId,
            productId,
            product.Name,
            cancellationToken);

        return Result<ProductPriceSummaryResponse>.Success(summary);
    }

    public async Task<Result<StorePriceSummaryResponse>> GetStorePriceSummaryAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        if (store is null)
        {
            return Result<StorePriceSummaryResponse>.Failure(DashboardErrors.StoreNotFound);
        }

        var summary = await dashboardRepository.GetStorePriceSummaryAsync(
            userId,
            storeId,
            store.Name,
            cancellationToken);

        return Result<StorePriceSummaryResponse>.Success(summary);
    }

    public async Task<Result<AlertSummaryResponse>> GetAlertSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var summary = await dashboardRepository.GetAlertSummaryAsync(userId, cancellationToken);

        return Result<AlertSummaryResponse>.Success(summary);
    }
}
