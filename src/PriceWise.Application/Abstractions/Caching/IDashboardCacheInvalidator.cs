namespace PriceWise.Application.Abstractions.Caching;

public interface IDashboardCacheInvalidator
{
    Task InvalidateUserDashboardAsync(Guid userId, CancellationToken cancellationToken = default);

    Task InvalidateProductSummaryAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task InvalidateStoreSummaryAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task InvalidateAlertSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
