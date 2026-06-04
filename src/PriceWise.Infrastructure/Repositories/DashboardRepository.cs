using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Dashboard.Dtos;

namespace PriceWise.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                (select count(*)::int from products where user_id = @UserId) as TotalProducts,
                (select count(*)::int from products where user_id = @UserId and is_active = true) as ActiveProducts,
                (select count(*)::int from products where user_id = @UserId and is_active = false) as InactiveProducts,
                (select count(*)::int from stores where user_id = @UserId) as TotalStores,
                (select count(*)::int from stores where user_id = @UserId and is_active = true) as ActiveStores,
                (select count(*)::int from stores where user_id = @UserId and is_active = false) as InactiveStores,
                (select count(*)::int from price_histories where user_id = @UserId) as TotalPriceHistories,
                (select count(*)::int from price_alerts where user_id = @UserId) as TotalPriceAlerts,
                (select count(*)::int from price_alerts where user_id = @UserId and is_active = true) as ActivePriceAlerts,
                (select count(*)::int from alert_notifications where user_id = @UserId) as TotalAlertNotifications,
                (select min(price) from price_histories where user_id = @UserId) as LowestPriceRegistered,
                (select max(price) from price_histories where user_id = @UserId) as HighestPriceRegistered,
                (select avg(price) from price_histories where user_id = @UserId) as AveragePriceRegistered,
                (select max(captured_at) from price_histories where user_id = @UserId) as LastPriceCapturedAt
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<DashboardSummaryResponse>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<ProductPriceSummaryResponse> GetProductPriceSummaryAsync(
        Guid userId,
        Guid productId,
        string productName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            with product_prices as (
                select price, captured_at
                from price_histories
                where user_id = @UserId and product_id = @ProductId
            ),
            latest_price as (
                select price, captured_at
                from product_prices
                order by captured_at desc
                limit 1
            ),
            first_price as (
                select price, captured_at
                from product_prices
                order by captured_at asc
                limit 1
            ),
            active_alert as (
                select target_price
                from price_alerts
                where user_id = @UserId and product_id = @ProductId and is_active = true
                order by created_at_utc desc
                limit 1
            )
            select
                @ProductId as ProductId,
                @ProductName as ProductName,
                (select count(*)::int from product_prices) as TotalPriceHistories,
                (select min(price) from product_prices) as LowestPrice,
                (select max(price) from product_prices) as HighestPrice,
                (select avg(price) from product_prices) as AveragePrice,
                (select price from latest_price) as LatestPrice,
                (select captured_at from latest_price) as LatestPriceCapturedAt,
                (select min(captured_at) from product_prices) as FirstPriceCapturedAt,
                case
                    when (select price from first_price) is null
                        or (select price from first_price) = 0
                        or (select price from latest_price) is null
                    then null
                    else (((select price from latest_price) - (select price from first_price))
                        / (select price from first_price)) * 100
                end as PriceVariationPercentage,
                exists(select 1 from active_alert) as HasActiveAlert,
                (select target_price from active_alert) as TargetAlertPrice
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<ProductPriceSummaryResponse>(
            new CommandDefinition(
                sql,
                new { UserId = userId, ProductId = productId, ProductName = productName },
                cancellationToken: cancellationToken));
    }

    public async Task<StorePriceSummaryResponse> GetStorePriceSummaryAsync(
        Guid userId,
        Guid storeId,
        string storeName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                @StoreId as StoreId,
                @StoreName as StoreName,
                count(distinct product_id)::int as TotalProductsMonitored,
                count(*)::int as TotalPriceHistories,
                min(price) as LowestPriceRegistered,
                max(price) as HighestPriceRegistered,
                avg(price) as AveragePriceRegistered,
                max(captured_at) as LastPriceCapturedAt
            from price_histories
            where user_id = @UserId and store_id = @StoreId
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<StorePriceSummaryResponse>(
            new CommandDefinition(
                sql,
                new { UserId = userId, StoreId = storeId, StoreName = storeName },
                cancellationToken: cancellationToken));
    }

    public async Task<AlertSummaryResponse> GetAlertSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                (select count(*)::int from price_alerts where user_id = @UserId) as TotalAlerts,
                (select count(*)::int from price_alerts where user_id = @UserId and is_active = true) as ActiveAlerts,
                (select count(*)::int from price_alerts where user_id = @UserId and is_active = false) as InactiveAlerts,
                (select count(*)::int from alert_notifications where user_id = @UserId) as TotalNotifications,
                (
                    select count(*)::int
                    from alert_notifications
                    where user_id = @UserId and triggered_at >= ((now() at time zone 'utc') - interval '7 days')
                ) as NotificationsLastSevenDays,
                (
                    select count(*)::int
                    from alert_notifications
                    where user_id = @UserId and triggered_at >= ((now() at time zone 'utc') - interval '30 days')
                ) as NotificationsLastThirtyDays,
                (select max(triggered_at) from alert_notifications where user_id = @UserId) as LastNotificationAt
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<AlertSummaryResponse>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }
}
