namespace PriceWise.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "PriceWise";

    public string Audience { get; init; } = "PriceWise";

    public string Secret { get; init; } = "pricewise-development-secret-key";

    public int AccessTokenExpirationMinutes { get; init; } = 60;

    public int RefreshTokenExpirationDays { get; init; } = 7;
}
