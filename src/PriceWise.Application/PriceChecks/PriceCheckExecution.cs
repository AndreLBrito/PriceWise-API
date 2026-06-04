namespace PriceWise.Application.PriceChecks;

public sealed record PriceCheckExecution(
    Guid Id,
    DateTime StartedAt,
    DateTime CompletedAt,
    string Status,
    string Message,
    int ProductsChecked,
    int HistoriesCreated,
    int ProductsSkipped,
    int ProductsFailed);
