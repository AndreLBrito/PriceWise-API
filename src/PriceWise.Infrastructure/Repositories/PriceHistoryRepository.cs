using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public PriceHistoryRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<PriceHistory>> ListByProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, store_id as StoreId,
                   price, currency, captured_at as CapturedAt, source_url as SourceUrl,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_histories
            where user_id = @UserId and product_id = @ProductId
            order by captured_at desc
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PriceHistoryRow>(
            new CommandDefinition(sql, new { UserId = userId, ProductId = productId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToPriceHistory()).ToArray();
    }

    public async Task<PriceHistory?> GetLatestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, store_id as StoreId,
                   price, currency, captured_at as CapturedAt, source_url as SourceUrl,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_histories
            where user_id = @UserId and product_id = @ProductId
            order by captured_at desc
            limit 1
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PriceHistoryRow>(
            new CommandDefinition(sql, new { UserId = userId, ProductId = productId }, cancellationToken: cancellationToken));

        return row?.ToPriceHistory();
    }

    public async Task<PriceHistory?> GetLowestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, store_id as StoreId,
                   price, currency, captured_at as CapturedAt, source_url as SourceUrl,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_histories
            where user_id = @UserId and product_id = @ProductId
            order by price asc, captured_at desc
            limit 1
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PriceHistoryRow>(
            new CommandDefinition(sql, new { UserId = userId, ProductId = productId }, cancellationToken: cancellationToken));

        return row?.ToPriceHistory();
    }

    public async Task<(decimal AveragePrice, int EntriesCount)?> GetAverageAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select avg(price) as AveragePrice, count(*)::int as EntriesCount
            from price_histories
            where user_id = @UserId and product_id = @ProductId
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<AveragePriceRow>(
            new CommandDefinition(sql, new { UserId = userId, ProductId = productId }, cancellationToken: cancellationToken));

        return row.EntriesCount == 0 ? null : (row.AveragePrice, row.EntriesCount);
    }

    public async Task<PriceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, product_id as ProductId, store_id as StoreId,
                   price, currency, captured_at as CapturedAt, source_url as SourceUrl,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from price_histories
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PriceHistoryRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToPriceHistory();
    }

    public async Task AddAsync(PriceHistory entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into price_histories (
                id, user_id, product_id, store_id, price, currency, captured_at, source_url, created_at_utc)
            values (
                @Id, @UserId, @ProductId, @StoreId, @Price, @Currency, @CapturedAt, @SourceUrl, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PriceHistory entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update price_histories
            set price = @Price,
                currency = @Currency,
                captured_at = @CapturedAt,
                source_url = @SourceUrl,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from price_histories where id = @Id";

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private sealed record PriceHistoryRow(
        Guid Id,
        Guid UserId,
        Guid ProductId,
        Guid StoreId,
        decimal Price,
        string Currency,
        DateTime CapturedAt,
        string? SourceUrl,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public PriceHistory ToPriceHistory()
        {
            return PriceHistory.Restore(
                Id,
                UserId,
                ProductId,
                StoreId,
                Price,
                Currency,
                CapturedAt,
                SourceUrl,
                CreatedAtUtc,
                UpdatedAtUtc);
        }
    }

    private sealed record AveragePriceRow(decimal AveragePrice, int EntriesCount);
}
