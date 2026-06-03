namespace PriceWise.Application.PriceHistories.Dtos;

public sealed record AveragePriceHistoryResponse(
    Guid ProductId,
    decimal AveragePrice,
    int EntriesCount);
