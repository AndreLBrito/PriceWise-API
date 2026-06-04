namespace PriceWise.Application.Abstractions.Caching;

public static class CacheKeys
{
    private const string Prefix = "pricewise";

    public static string DashboardUserPrefix(Guid userId)
    {
        return $"{Prefix}:dashboard:user:{userId:N}";
    }

    public static string DashboardSummary(Guid userId)
    {
        return $"{DashboardUserPrefix(userId)}:summary";
    }

    public static string DashboardProductSummary(Guid userId, Guid productId)
    {
        return $"{DashboardUserPrefix(userId)}:products:{productId:N}:summary";
    }

    public static string DashboardStoreSummary(Guid userId, Guid storeId)
    {
        return $"{DashboardUserPrefix(userId)}:stores:{storeId:N}:summary";
    }

    public static string DashboardAlertSummary(Guid userId)
    {
        return $"{DashboardUserPrefix(userId)}:alerts:summary";
    }
}
