using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class StoreRepository : IStoreRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public StoreRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Store>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, base_url as BaseUrl, logo_url as LogoUrl,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from stores
            where user_id = @UserId and is_active = true
            order by name
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StoreRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToStore()).ToArray();
    }

    public async Task<PagedResponse<Store>> ListByUserIdAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var sortColumn = GetSortColumn(request.SortBy);
        var sortDirection = request.IsDescending ? "desc" : "asc";
        var whereSql = """
            where user_id = @UserId
              and (@IsActive is null or is_active = @IsActive)
              and (cast(@StartDate as timestamp) is null or created_at_utc >= @StartDate)
              and (cast(@EndDate as timestamp) is null or created_at_utc <= @EndDate)
              and (
                  cast(@Search as text) is null
                  or name ilike @Search
                  or base_url ilike @Search
              )
            """;
        var countSql = $"select count(*) from stores {whereSql}";
        var listSql = $"""
            select id, user_id as UserId, name, base_url as BaseUrl, logo_url as LogoUrl,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from stores
            {whereSql}
            order by {sortColumn} {sortDirection}
            limit @PageSize offset @Offset
            """;

        var parameters = new
        {
            UserId = userId,
            IsActive = request.IsActive ?? true,
            request.StartDate,
            request.EndDate,
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%",
            PageSize = request.NormalizedPageSize,
            request.Offset
        };

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var totalItems = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<StoreRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return PagedResponse<Store>.Create(
            rows.Select(row => row.ToStore()).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            totalItems);
    }

    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, base_url as BaseUrl, logo_url as LogoUrl,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from stores
            where id = @Id and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StoreRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToStore();
    }

    public async Task<Store?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, base_url as BaseUrl, logo_url as LogoUrl,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from stores
            where id = @Id and user_id = @UserId and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StoreRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        return row?.ToStore();
    }

    public async Task<Store?> GetByBaseUrlAsync(
        Guid userId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, base_url as BaseUrl, logo_url as LogoUrl,
                   is_active as IsActive, created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from stores
            where user_id = @UserId and base_url = @BaseUrl and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StoreRow>(
            new CommandDefinition(
                sql,
                new { UserId = userId, BaseUrl = baseUrl },
                cancellationToken: cancellationToken));

        return row?.ToStore();
    }

    public async Task AddAsync(Store entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into stores (id, user_id, name, base_url, logo_url, is_active, created_at_utc)
            values (@Id, @UserId, @Name, @BaseUrl, @LogoUrl, @IsActive, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Store entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update stores
            set name = @Name,
                base_url = @BaseUrl,
                logo_url = @LogoUrl,
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
            update stores
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

    private sealed record StoreRow(
        Guid Id,
        Guid UserId,
        string Name,
        string BaseUrl,
        string? LogoUrl,
        bool IsActive,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public Store ToStore()
        {
            return Store.Restore(
                Id,
                UserId,
                Name,
                BaseUrl,
                LogoUrl,
                IsActive,
                CreatedAtUtc,
                UpdatedAtUtc);
        }
    }

    private static string GetSortColumn(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => "name",
            "baseurl" => "base_url",
            "createdat" => "created_at_utc",
            "updatedat" => "updated_at_utc",
            "isactive" => "is_active",
            _ => "created_at_utc"
        };
    }
}
