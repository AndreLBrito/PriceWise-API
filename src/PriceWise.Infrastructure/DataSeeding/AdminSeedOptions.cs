namespace PriceWise.Infrastructure.DataSeeding;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; set; } = true;

    public string Email { get; set; } = "admin@pricewise.com";

    public string Password { get; set; } = "Admin@123456";
}
