using PriceWise.Application.Dashboard.Dtos;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ProductPriceSummaryResponse> GetProductPriceSummaryAsync(
        Guid userId,
        Guid productId,
        string productName,
        CancellationToken cancellationToken = default);

    Task<StorePriceSummaryResponse> GetStorePriceSummaryAsync(
        Guid userId,
        Guid storeId,
        string storeName,
        CancellationToken cancellationToken = default);

    Task<AlertSummaryResponse> GetAlertSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
