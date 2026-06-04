using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Exports.Dtos;

namespace PriceWise.Infrastructure.Repositories;

public sealed class ExportRepository : IExportRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public ExportRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<ProductExportRow>> ListProductsAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id,
                   name,
                   description,
                   brand,
                   category,
                   product_url as ProductUrl,
                   image_url as ImageUrl,
                   is_active as IsActive,
                   created_at_utc as CreatedAtUtc,
                   updated_at_utc as UpdatedAtUtc
            from products
            where user_id = @UserId
              and (@StartDate is null or created_at_utc >= @StartDate)
              and (@EndDate is null or created_at_utc <= @EndDate)
              and (@ProductId is null or id = @ProductId)
            order by created_at_utc desc
            limit @MaxRows
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProductExportRow>(
            new CommandDefinition(sql, CreateParameters(userId, filter, maxRows), cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<StoreExportRow>> ListStoresAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id,
                   name,
                   base_url as BaseUrl,
                   logo_url as LogoUrl,
                   is_active as IsActive,
                   created_at_utc as CreatedAtUtc,
                   updated_at_utc as UpdatedAtUtc
            from stores
            where user_id = @UserId
              and (@StartDate is null or created_at_utc >= @StartDate)
              and (@EndDate is null or created_at_utc <= @EndDate)
              and (@StoreId is null or id = @StoreId)
            order by created_at_utc desc
            limit @MaxRows
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StoreExportRow>(
            new CommandDefinition(sql, CreateParameters(userId, filter, maxRows), cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<PriceHistoryExportRow>> ListPriceHistoriesAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ph.id,
                   ph.product_id as ProductId,
                   p.name as ProductName,
                   ph.store_id as StoreId,
                   s.name as StoreName,
                   ph.price,
                   ph.currency,
                   ph.captured_at as CapturedAt,
                   ph.source_url as SourceUrl,
                   ph.created_at_utc as CreatedAtUtc
            from price_histories ph
            inner join products p on p.id = ph.product_id and p.user_id = ph.user_id
            inner join stores s on s.id = ph.store_id and s.user_id = ph.user_id
            where ph.user_id = @UserId
              and (@StartDate is null or ph.captured_at >= @StartDate)
              and (@EndDate is null or ph.captured_at <= @EndDate)
              and (@ProductId is null or ph.product_id = @ProductId)
              and (@StoreId is null or ph.store_id = @StoreId)
            order by ph.captured_at desc
            limit @MaxRows
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PriceHistoryExportRow>(
            new CommandDefinition(sql, CreateParameters(userId, filter, maxRows), cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<AlertNotificationExportRow>> ListAlertNotificationsAsync(
        Guid userId,
        ExportFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select an.id,
                   an.product_id as ProductId,
                   p.name as ProductName,
                   an.price_alert_id as PriceAlertId,
                   an.price_history_id as PriceHistoryId,
                   an.triggered_price as TriggeredPrice,
                   an.target_price as TargetPrice,
                   an.triggered_at as TriggeredAt,
                   an.created_at_utc as CreatedAtUtc
            from alert_notifications an
            inner join products p on p.id = an.product_id and p.user_id = an.user_id
            inner join price_histories ph on ph.id = an.price_history_id and ph.user_id = an.user_id
            where an.user_id = @UserId
              and (@StartDate is null or an.triggered_at >= @StartDate)
              and (@EndDate is null or an.triggered_at <= @EndDate)
              and (@ProductId is null or an.product_id = @ProductId)
              and (@StoreId is null or ph.store_id = @StoreId)
            order by an.triggered_at desc
            limit @MaxRows
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AlertNotificationExportRow>(
            new CommandDefinition(sql, CreateParameters(userId, filter, maxRows), cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    private static object CreateParameters(Guid userId, ExportFilter filter, int maxRows)
    {
        return new
        {
            UserId = userId,
            filter.StartDate,
            filter.EndDate,
            filter.ProductId,
            filter.StoreId,
            MaxRows = maxRows
        };
    }
}
