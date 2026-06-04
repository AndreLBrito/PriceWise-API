namespace PriceWise.Application.PriceChecks;

public sealed class PriceCheckOptions
{
    public const string SectionName = "PriceCheck";

    public bool Enabled { get; set; } = true;

    public int IntervalInMinutes { get; set; } = 30;

    public int MaxProductsPerExecution { get; set; } = 50;
}
