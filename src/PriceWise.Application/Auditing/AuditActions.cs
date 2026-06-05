namespace PriceWise.Application.Auditing;

public static class AuditActions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string ChangePassword = "ChangePassword";
    public const string RevokeRefreshTokens = "RevokeRefreshTokens";
    public const string ChangeRole = "ChangeRole";
    public const string Activate = "Activate";
    public const string Deactivate = "Deactivate";
    public const string ManualPriceCheck = "ManualPriceCheck";
    public const string WebhookSent = "WebhookSent";
    public const string WebhookFailed = "WebhookFailed";
    public const string EmailSent = "EmailSent";
    public const string EmailFailed = "EmailFailed";
}
