using Microsoft.Extensions.Options;

namespace PriceWise.Application.PriceChecks;

public sealed class MockPriceProvider : IPriceProvider
{
    private readonly PriceProviderOptions options;

    public MockPriceProvider(IOptions<PriceProviderOptions> options)
    {
        this.options = options.Value;
    }

    public Task<decimal> GetCurrentPriceAsync(
        PriceCheckCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        var minimum = Math.Max(0.01m, options.MinimumBasePrice);
        var maximum = Math.Max(minimum, options.MaximumBasePrice);
        var basePrice = candidate.LatestPrice is > 0
            ? candidate.LatestPrice.Value
            : Random.Shared.Next((int)minimum, (int)Math.Ceiling(maximum));
        var variationLimit = Math.Clamp(options.VariationPercentage, 0.01m, 0.20m);
        var variationPercentage = ((decimal)Random.Shared.NextDouble() * (variationLimit * 2)) - variationLimit;
        var price = decimal.Round(basePrice * (1 + variationPercentage), 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(Math.Max(price, 0.01m));
    }
}
