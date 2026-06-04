namespace PriceWise.Application.Authentication;

public sealed class AuthenticationSecurityOptions
{
    public const string SectionName = "AuthenticationSecurity";

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;
}
