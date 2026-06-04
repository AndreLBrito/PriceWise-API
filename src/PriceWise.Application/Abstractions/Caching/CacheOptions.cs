namespace PriceWise.Application.Abstractions.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; } = true;

    public string ConnectionString { get; set; } = "localhost:6379";

    public int DefaultExpirationInMinutes { get; set; } = 10;
}
