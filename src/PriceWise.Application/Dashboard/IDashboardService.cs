using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.Dashboard.Dtos;

namespace PriceWise.Application.Dashboard;

public interface IDashboardService : IService
{
    Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<ProductPriceSummaryResponse>> GetProductPriceSummaryAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Result<StorePriceSummaryResponse>> GetStorePriceSummaryAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<Result<AlertSummaryResponse>> GetAlertSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
