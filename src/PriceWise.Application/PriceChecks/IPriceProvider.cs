using PriceWise.Application.Abstractions.Services;

namespace PriceWise.Application.PriceChecks;

public interface IPriceProvider : IService
{
    Task<decimal> GetCurrentPriceAsync(
        PriceCheckCandidate candidate,
        CancellationToken cancellationToken = default);
}
