using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class PriceAlertRepository : IPriceAlertRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public PriceAlertRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<PriceAlert>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, target_price as TargetPrice,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_alerts
            where user_id = @UserId and is_active = true
            order by created_at_utc desc
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PriceAlertRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToPriceAlert()).ToArray();
    }

    public async Task<PriceAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, target_price as TargetPrice,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_alerts
            where id = @Id and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PriceAlertRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToPriceAlert();
    }

    public async Task<PriceAlert?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, target_price as TargetPrice,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_alerts
            where id = @Id and user_id = @UserId and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PriceAlertRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        return row?.ToPriceAlert();
    }

    public async Task<PriceAlert?> GetActiveByProductIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, target_price as TargetPrice,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_alerts
            where user_id = @UserId and product_id = @ProductId and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PriceAlertRow>(
            new CommandDefinition(
                sql,
                new { UserId = userId, ProductId = productId },
                cancellationToken: cancellationToken));

        return row?.ToPriceAlert();
    }

    public async Task<IReadOnlyCollection<PriceAlert>> ListActiveByProductIdAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, target_price as TargetPrice,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_alerts
            where user_id = @UserId and product_id = @ProductId and is_active = true
            order by created_at_utc desc
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PriceAlertRow>(
            new CommandDefinition(
                sql,
                new { UserId = userId, ProductId = productId },
                cancellationToken: cancellationToken));

        return rows.Select(row => row.ToPriceAlert()).ToArray();
    }

    public async Task AddAsync(PriceAlert entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into price_alerts (id, user_id, product_id, target_price, is_active, created_at_utc)
            values (@Id, @UserId, @ProductId, @TargetPrice, @IsActive, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PriceAlert entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update price_alerts
            set target_price = @TargetPrice,
                is_active = @IsActive,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id and user_id = @UserId
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update price_alerts
            set is_active = false,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, UpdatedAtUtc = DateTime.UtcNow },
            cancellationToken: cancellationToken));
    }

    private sealed record PriceAlertRow(
        Guid Id,
        Guid UserId,
        Guid ProductId,
        decimal TargetPrice,
        bool IsActive,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public PriceAlert ToPriceAlert()
        {
            return PriceAlert.Restore(
                Id,
                UserId,
                ProductId,
                TargetPrice,
                IsActive,
                CreatedAtUtc,
                UpdatedAtUtc);
        }
    }
}
