namespace PriceWise.Application.PriceChecks.Dtos;

public sealed record PriceCheckRunResponse(
    DateTime ExecutedAt,
    string Status,
    string Message,
    int ProductsChecked,
    int HistoriesCreated,
    int ProductsSkipped,
    int ProductsFailed);
