namespace PriceWise.Api.RateLimiting;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    public int LoginPermitLimit { get; set; } = 5;

    public int LoginWindowInMinutes { get; set; } = 1;

    public int RefreshTokenPermitLimit { get; set; } = 10;

    public int RefreshTokenWindowInMinutes { get; set; } = 1;

    public int GeneralPermitLimit { get; set; } = 100;

    public int GeneralWindowInMinutes { get; set; } = 1;

    public int PriceCheckPermitLimit { get; set; } = 3;

    public int PriceCheckWindowInMinutes { get; set; } = 5;
}
