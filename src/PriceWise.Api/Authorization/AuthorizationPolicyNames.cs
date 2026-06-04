namespace PriceWise.Api.Authorization;

public static class AuthorizationPolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string PriceCheckManagement = "PriceCheckManagement";
    public const string SystemManagement = "SystemManagement";
    public const string TelemetryManagement = "TelemetryManagement";
    public const string SeedManagement = "SeedManagement";
}
