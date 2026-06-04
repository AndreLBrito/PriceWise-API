namespace PriceWise.Api.RateLimiting;

public static class RateLimitPolicyNames
{
    public const string Login = "login";

    public const string RefreshToken = "refresh-token";

    public const string General = "general";

    public const string PriceCheck = "price-check";
}
