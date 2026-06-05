namespace PriceWise.Application.PriceChecks;

public sealed class PriceProviderOptions
{
    public const string SectionName = "PriceProvider";

    public decimal MinimumBasePrice { get; set; } = 50;

    public decimal MaximumBasePrice { get; set; } = 1500;

    public decimal VariationPercentage { get; set; } = 0.03m;
}
