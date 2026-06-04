namespace PriceWise.Application.Abstractions.Caching;

public sealed class NoOpDashboardCacheInvalidator : IDashboardCacheInvalidator
{
    public Task InvalidateUserDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task InvalidateProductSummaryAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task InvalidateStoreSummaryAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task InvalidateAlertSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
