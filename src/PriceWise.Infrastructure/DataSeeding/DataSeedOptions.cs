namespace PriceWise.Infrastructure.DataSeeding;

public sealed class DataSeedOptions
{
    public const string SectionName = "DataSeed";

    public bool Enabled { get; set; } = true;

    public bool CreateDemoUser { get; set; } = true;

    public string DemoUserEmail { get; set; } = "demo@pricewise.com";

    public string DemoUserPassword { get; set; } = "Demo@123456";

    public bool CreateDemoData { get; set; } = true;
}
