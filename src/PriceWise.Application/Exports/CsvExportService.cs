using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.Exports.Dtos;

namespace PriceWise.Application.Exports;

public sealed class CsvExportService : IExportService
{
    private const string ContentType = "text/csv; charset=utf-8";
    private readonly IExportRepository exportRepository;
    private readonly IOptions<CsvExportOptions> options;

    public CsvExportService(
        IExportRepository exportRepository,
        IOptions<CsvExportOptions> options)
    {
        this.exportRepository = exportRepository;
        this.options = options;
    }

    public async Task<Result<CsvExportResponse>> ExportProductsAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var exportOptions = Normalize(options.Value);
        var rows = await exportRepository.ListProductsAsync(userId, filter, exportOptions.MaxRows, cancellationToken);
        var csv = CsvWriter.Write(
            ["Id", "Name", "Description", "Brand", "Category", "ProductUrl", "ImageUrl", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"],
            rows.Select(row => new[]
            {
                row.Id.ToString(),
                row.Name,
                row.Description,
                row.Brand,
                row.Category,
                row.ProductUrl,
                row.ImageUrl,
                row.IsActive.ToString(CultureInfo.InvariantCulture),
                FormatDate(row.CreatedAtUtc, exportOptions),
                FormatDate(row.UpdatedAtUtc, exportOptions)
            }));

        return Result<CsvExportResponse>.Success(new CsvExportResponse(
            BuildFileName("produtos"),
            ContentType,
            csv));
    }

    public async Task<Result<CsvExportResponse>> ExportStoresAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var exportOptions = Normalize(options.Value);
        var rows = await exportRepository.ListStoresAsync(userId, filter, exportOptions.MaxRows, cancellationToken);
        var csv = CsvWriter.Write(
            ["Id", "Name", "BaseUrl", "LogoUrl", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"],
            rows.Select(row => new[]
            {
                row.Id.ToString(),
                row.Name,
                row.BaseUrl,
                row.LogoUrl,
                row.IsActive.ToString(CultureInfo.InvariantCulture),
                FormatDate(row.CreatedAtUtc, exportOptions),
                FormatDate(row.UpdatedAtUtc, exportOptions)
            }));

        return Result<CsvExportResponse>.Success(new CsvExportResponse(
            BuildFileName("lojas"),
            ContentType,
            csv));
    }

    public async Task<Result<CsvExportResponse>> ExportPriceHistoriesAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var exportOptions = Normalize(options.Value);
        var rows = await exportRepository.ListPriceHistoriesAsync(userId, filter, exportOptions.MaxRows, cancellationToken);
        var csv = CsvWriter.Write(
            ["Id", "ProductId", "ProductName", "StoreId", "StoreName", "Price", "Currency", "CapturedAt", "SourceUrl", "CreatedAtUtc"],
            rows.Select(row => new[]
            {
                row.Id.ToString(),
                row.ProductId.ToString(),
                row.ProductName,
                row.StoreId.ToString(),
                row.StoreName,
                FormatDecimal(row.Price),
                row.Currency,
                FormatDate(row.CapturedAt, exportOptions),
                row.SourceUrl,
                FormatDate(row.CreatedAtUtc, exportOptions)
            }));

        return Result<CsvExportResponse>.Success(new CsvExportResponse(
            BuildFileName("historico-precos"),
            ContentType,
            csv));
    }

    public async Task<Result<CsvExportResponse>> ExportAlertNotificationsAsync(
        Guid userId,
        ExportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var exportOptions = Normalize(options.Value);
        var rows = await exportRepository.ListAlertNotificationsAsync(userId, filter, exportOptions.MaxRows, cancellationToken);
        var csv = CsvWriter.Write(
            ["Id", "ProductId", "ProductName", "PriceAlertId", "PriceHistoryId", "TriggeredPrice", "TargetPrice", "TriggeredAt", "CreatedAtUtc"],
            rows.Select(row => new[]
            {
                row.Id.ToString(),
                row.ProductId.ToString(),
                row.ProductName,
                row.PriceAlertId.ToString(),
                row.PriceHistoryId.ToString(),
                FormatDecimal(row.TriggeredPrice),
                FormatDecimal(row.TargetPrice),
                FormatDate(row.TriggeredAt, exportOptions),
                FormatDate(row.CreatedAtUtc, exportOptions)
            }));

        return Result<CsvExportResponse>.Success(new CsvExportResponse(
            BuildFileName("notificacoes-alerta"),
            ContentType,
            csv));
    }

    private static string BuildFileName(string prefix)
    {
        return $"pricewise-{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
    }

    private static string FormatDate(DateTime? value, CsvExportOptions options)
    {
        return value?.ToUniversalTime().ToString(options.DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static CsvExportOptions Normalize(CsvExportOptions options)
    {
        return new CsvExportOptions
        {
            MaxRows = options.MaxRows <= 0 ? 10_000 : options.MaxRows,
            DateFormat = string.IsNullOrWhiteSpace(options.DateFormat)
                ? "yyyy-MM-dd HH:mm:ss"
                : options.DateFormat
        };
    }

    private static class CsvWriter
    {
        public static string Write(
            IReadOnlyCollection<string> headers,
            IEnumerable<IReadOnlyCollection<string?>> rows)
        {
            var builder = new StringBuilder();
            AppendLine(builder, headers);

            foreach (var row in rows)
            {
                AppendLine(builder, row);
            }

            return builder.ToString();
        }

        private static void AppendLine(StringBuilder builder, IReadOnlyCollection<string?> values)
        {
            builder.AppendJoin(',', values.Select(Escape));
            builder.AppendLine();
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var mustQuote = value.Contains(',')
                || value.Contains('"')
                || value.Contains('\r')
                || value.Contains('\n');

            if (!mustQuote)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
