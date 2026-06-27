using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Product>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, description, brand, category,
                   product_url as ProductUrl, image_url as ImageUrl, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from products
            where user_id = @UserId and is_active = true
            order by name
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProductRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToProduct()).ToArray();
    }

    public async Task<PagedResponse<Product>> ListByUserIdAsync(
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
                  or coalesce(description, '') ilike @Search
                  or coalesce(brand, '') ilike @Search
                  or coalesce(category, '') ilike @Search
                  or product_url ilike @Search
              )
            """;
        var countSql = $"select count(*) from products {whereSql}";
        var listSql = $"""
            select id, user_id as UserId, name, description, brand, category,
                   product_url as ProductUrl, image_url as ImageUrl, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from products
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
        var rows = await connection.QueryAsync<ProductRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return PagedResponse<Product>.Create(
            rows.Select(row => row.ToProduct()).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            totalItems);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, description, brand, category,
                   product_url as ProductUrl, image_url as ImageUrl, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from products
            where id = @Id and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToProduct();
    }

    public async Task<Product?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, description, brand, category,
                   product_url as ProductUrl, image_url as ImageUrl, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from products
            where id = @Id and user_id = @UserId and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        return row?.ToProduct();
    }

    public async Task<Product?> GetByProductUrlAsync(
        Guid userId,
        string productUrl,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, name, description, brand, category,
                   product_url as ProductUrl, image_url as ImageUrl, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from products
            where user_id = @UserId and product_url = @ProductUrl and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            new CommandDefinition(
                sql,
                new { UserId = userId, ProductUrl = productUrl },
                cancellationToken: cancellationToken));

        return row?.ToProduct();
    }

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into products (
                id, user_id, name, description, brand, category, product_url, image_url,
                is_active, created_at_utc)
            values (
                @Id, @UserId, @Name, @Description, @Brand, @Category, @ProductUrl, @ImageUrl,
                @IsActive, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update products
            set name = @Name,
                description = @Description,
                brand = @Brand,
                category = @Category,
                product_url = @ProductUrl,
                image_url = @ImageUrl,
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
            update products
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

    private sealed record ProductRow(
        Guid Id,
        Guid UserId,
        string Name,
        string? Description,
        string? Brand,
        string? Category,
        string ProductUrl,
        string? ImageUrl,
        bool IsActive,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public Product ToProduct()
        {
            return Product.Restore(
                Id,
                UserId,
                Name,
                Description,
                Brand,
                Category,
                ProductUrl,
                ImageUrl,
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
            "brand" => "brand",
            "category" => "category",
            "createdat" => "created_at_utc",
            "updatedat" => "updated_at_utc",
            "isactive" => "is_active",
            _ => "created_at_utc"
        };
    }
}
