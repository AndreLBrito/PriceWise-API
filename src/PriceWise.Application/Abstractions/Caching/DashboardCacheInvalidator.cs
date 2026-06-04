namespace PriceWise.Application.Abstractions.Caching;

public sealed class DashboardCacheInvalidator : IDashboardCacheInvalidator
{
    private readonly ICacheService cacheService;

    public DashboardCacheInvalidator(ICacheService cacheService)
    {
        this.cacheService = cacheService;
    }

    public async Task InvalidateUserDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await cacheService.RemoveAsync(CacheKeys.DashboardSummary(userId), cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.DashboardAlertSummary(userId), cancellationToken);
    }

    public async Task InvalidateProductSummaryAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await cacheService.RemoveAsync(CacheKeys.DashboardSummary(userId), cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.DashboardProductSummary(userId, productId), cancellationToken);
    }

    public async Task InvalidateStoreSummaryAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        await cacheService.RemoveAsync(CacheKeys.DashboardSummary(userId), cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.DashboardStoreSummary(userId, storeId), cancellationToken);
    }

    public async Task InvalidateAlertSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await cacheService.RemoveAsync(CacheKeys.DashboardSummary(userId), cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.DashboardAlertSummary(userId), cancellationToken);
    }
}
