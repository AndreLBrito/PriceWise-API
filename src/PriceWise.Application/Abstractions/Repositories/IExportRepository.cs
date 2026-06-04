using PriceWise.Application.Exports.Dtos;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IExportRepository
{
    Task<IReadOnlyCollection<ProductExportRow>> ListProductsAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StoreExportRow>> ListStoresAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PriceHistoryExportRow>> ListPriceHistoriesAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AlertNotificationExportRow>> ListAlertNotificationsAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default);
}
