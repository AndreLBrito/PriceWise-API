using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.PriceChecks;

namespace PriceWise.Infrastructure.Repositories;

public sealed class PriceCheckRepository : IPriceCheckRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public PriceCheckRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<PriceCheckCandidate>> ListCandidatesAsync(
        int maxProducts,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                p.user_id as UserId,
                p.id as ProductId,
                selected_store.store_id as StoreId,
                p.product_url as ProductUrl,
                latest_history.price as LatestPrice,
                latest_history.captured_at as LatestCapturedAt
            from products p
            left join lateral (
                select ph.store_id, ph.price, ph.captured_at
                from price_histories ph
                where ph.user_id = p.user_id and ph.product_id = p.id
                order by ph.captured_at desc
                limit 1
            ) latest_history on true
            join lateral (
                select coalesce(
                    latest_history.store_id,
                    (
                        select s.id
                        from stores s
                        where s.user_id = p.user_id and s.is_active = true
                        order by s.name
                        limit 1
                    )
                ) as store_id
            ) selected_store on selected_store.store_id is not null
            join stores active_store
                on active_store.id = selected_store.store_id
                and active_store.user_id = p.user_id
                and active_store.is_active = true
            where p.is_active = true
            order by latest_history.captured_at nulls first, p.created_at_utc
            limit @MaxProducts
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var candidates = await connection.QueryAsync<PriceCheckCandidate>(
            new CommandDefinition(sql, new { MaxProducts = maxProducts }, cancellationToken: cancellationToken));

        return candidates.ToArray();
    }

    public async Task AddExecutionAsync(
        PriceCheckExecution execution,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into price_check_executions (
                id, started_at, completed_at, status, message, products_checked,
                histories_created, products_skipped, products_failed, created_at_utc)
            values (
                @Id, @StartedAt, @CompletedAt, @Status, @Message, @ProductsChecked,
                @HistoriesCreated, @ProductsSkipped, @ProductsFailed, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                execution.Id,
                execution.StartedAt,
                execution.CompletedAt,
                execution.Status,
                execution.Message,
                execution.ProductsChecked,
                execution.HistoriesCreated,
                execution.ProductsSkipped,
                execution.ProductsFailed,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    public async Task<PriceCheckExecution?> GetLastExecutionAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, started_at as StartedAt, completed_at as CompletedAt, status, message,
                   products_checked as ProductsChecked, histories_created as HistoriesCreated,
                   products_skipped as ProductsSkipped, products_failed as ProductsFailed
            from price_check_executions
            order by completed_at desc
            limit 1
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PriceCheckExecution>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
