namespace PriceWise.Application.PriceChecks;

public sealed record PriceCheckCandidate(
    Guid UserId,
    Guid ProductId,
    Guid StoreId,
    string ProductUrl,
    decimal? LatestPrice,
    DateTime? LatestCapturedAt);
