namespace PriceWise.Application.Exports.Dtos;

public sealed record ExportFilter(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? ProductId = null,
    Guid? StoreId = null);
