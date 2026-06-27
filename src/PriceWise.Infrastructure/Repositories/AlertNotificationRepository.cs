using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class AlertNotificationRepository : IAlertNotificationRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public AlertNotificationRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<AlertNotification>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, price_alert_id as PriceAlertId, product_id as ProductId,
                   price_history_id as PriceHistoryId, triggered_price as TriggeredPrice,
                   target_price as TargetPrice, triggered_at as TriggeredAt,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from alert_notifications
            where user_id = @UserId
            order by triggered_at desc
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AlertNotificationRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToAlertNotification()).ToArray();
    }

    public async Task<PagedResponse<AlertNotification>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var sortColumn = GetSortColumn(request.SortBy);
        var sortDirection = request.IsDescending ? "desc" : "asc";
        var whereSql = """
            where user_id = @UserId
              and (cast(@StartDate as timestamp) is null or triggered_at >= @StartDate)
              and (cast(@EndDate as timestamp) is null or triggered_at <= @EndDate)
            """;
        var countSql = $"select count(*) from alert_notifications {whereSql}";
        var listSql = $"""
            select id, user_id as UserId, price_alert_id as PriceAlertId, product_id as ProductId,
                   price_history_id as PriceHistoryId, triggered_price as TriggeredPrice,
                   target_price as TargetPrice, triggered_at as TriggeredAt,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from alert_notifications
            {whereSql}
            order by {sortColumn} {sortDirection}
            limit @PageSize offset @Offset
            """;
        var parameters = new
        {
            UserId = userId,
            request.StartDate,
            request.EndDate,
            PageSize = request.NormalizedPageSize,
            request.Offset
        };

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var totalItems = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<AlertNotificationRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return PagedResponse<AlertNotification>.Create(
            rows.Select(row => row.ToAlertNotification()).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            totalItems);
    }

    public async Task<AlertNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, price_alert_id as PriceAlertId, product_id as ProductId,
                   price_history_id as PriceHistoryId, triggered_price as TriggeredPrice,
                   target_price as TargetPrice, triggered_at as TriggeredAt,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from alert_notifications
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AlertNotificationRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToAlertNotification();
    }

    public async Task<AlertNotification?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, price_alert_id as PriceAlertId, product_id as ProductId,
                   price_history_id as PriceHistoryId, triggered_price as TriggeredPrice,
                   target_price as TargetPrice, triggered_at as TriggeredAt,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from alert_notifications
            where id = @Id and user_id = @UserId
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AlertNotificationRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        return row?.ToAlertNotification();
    }

    public async Task<bool> ExistsAsync(
        Guid priceAlertId,
        Guid priceHistoryId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select exists (
                select 1
                from alert_notifications
                where price_alert_id = @PriceAlertId and price_history_id = @PriceHistoryId
            )
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { PriceAlertId = priceAlertId, PriceHistoryId = priceHistoryId },
                cancellationToken: cancellationToken));
    }

    public async Task AddAsync(AlertNotification entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into alert_notifications (
                id, user_id, price_alert_id, product_id, price_history_id,
                triggered_price, target_price, triggered_at, created_at_utc)
            values (
                @Id, @UserId, @PriceAlertId, @ProductId, @PriceHistoryId,
                @TriggeredPrice, @TargetPrice, @TriggeredAt, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(AlertNotification entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update alert_notifications
            set updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from alert_notifications where id = @Id";

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private sealed record AlertNotificationRow(
        Guid Id,
        Guid UserId,
        Guid PriceAlertId,
        Guid ProductId,
        Guid PriceHistoryId,
        decimal TriggeredPrice,
        decimal TargetPrice,
        DateTime TriggeredAt,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public AlertNotification ToAlertNotification()
        {
            return AlertNotification.Restore(
                Id,
                UserId,
                PriceAlertId,
                ProductId,
                PriceHistoryId,
                TriggeredPrice,
                TargetPrice,
                TriggeredAt,
                CreatedAtUtc,
                UpdatedAtUtc);
        }
    }

    private static string GetSortColumn(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "triggeredprice" => "triggered_price",
            "targetprice" => "target_price",
            "triggeredat" => "triggered_at",
            "createdat" => "created_at_utc",
            _ => "created_at_utc"
        };
    }
}
