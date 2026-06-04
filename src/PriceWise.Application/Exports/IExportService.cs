using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.Exports.Dtos;

namespace PriceWise.Application.Exports;

public interface IExportService : IService
{
    Task<Result<CsvExportResponse>> ExportProductsAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<CsvExportResponse>> ExportStoresAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<CsvExportResponse>> ExportPriceHistoriesAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<CsvExportResponse>> ExportAlertNotificationsAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default);
}
