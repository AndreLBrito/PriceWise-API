using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.Dashboard.Dtos;

namespace PriceWise.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository dashboardRepository;
    private readonly IProductRepository productRepository;
    private readonly IStoreRepository storeRepository;
    private readonly ICacheService cacheService;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        ICacheService cacheService)
    {
        this.dashboardRepository = dashboardRepository;
        this.productRepository = productRepository;
        this.storeRepository = storeRepository;
        this.cacheService = cacheService;
    }

    public async Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var summary = await cacheService.GetOrCreateAsync(
            CacheKeys.DashboardSummary(userId),
            token => dashboardRepository.GetSummaryAsync(userId, token),
            cancellationToken: cancellationToken);

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

        var summary = await cacheService.GetOrCreateAsync(
            CacheKeys.DashboardProductSummary(userId, productId),
            token => dashboardRepository.GetProductPriceSummaryAsync(userId, productId, product.Name, token),
            cancellationToken: cancellationToken);

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

        var summary = await cacheService.GetOrCreateAsync(
            CacheKeys.DashboardStoreSummary(userId, storeId),
            token => dashboardRepository.GetStorePriceSummaryAsync(userId, storeId, store.Name, token),
            cancellationToken: cancellationToken);

        return Result<StorePriceSummaryResponse>.Success(summary);
    }

    public async Task<Result<AlertSummaryResponse>> GetAlertSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var summary = await cacheService.GetOrCreateAsync(
            CacheKeys.DashboardAlertSummary(userId),
            token => dashboardRepository.GetAlertSummaryAsync(userId, token),
            cancellationToken: cancellationToken);

        return Result<AlertSummaryResponse>.Success(summary);
    }
}
