using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Infrastructure.Repositories;

public sealed class NotificationChannelRepository : INotificationChannelRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public NotificationChannelRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<NotificationChannel>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, type, name, destination, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from notification_channels
            where user_id = @UserId and is_active = true
            order by name
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<NotificationChannelRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToNotificationChannel()).ToArray();
    }

    public async Task<PagedResponse<NotificationChannel>> ListByUserIdAsync(
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
                  or destination ilike @Search
                  or type ilike @Search
              )
            """;
        var countSql = $"select count(*) from notification_channels {whereSql}";
        var listSql = $"""
            select id, user_id as UserId, type, name, destination, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from notification_channels
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
        var rows = await connection.QueryAsync<NotificationChannelRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return PagedResponse<NotificationChannel>.Create(
            rows.Select(row => row.ToNotificationChannel()).ToArray(),
            request.NormalizedPage,
            request.NormalizedPageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<NotificationChannel>> ListActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, type, name, destination, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from notification_channels
            where user_id = @UserId and is_active = true
            order by name
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<NotificationChannelRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToNotificationChannel()).ToArray();
    }

    public async Task<NotificationChannel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, type, name, destination, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from notification_channels
            where id = @Id and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<NotificationChannelRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToNotificationChannel();
    }

    public async Task<NotificationChannel?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, type, name, destination, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from notification_channels
            where id = @Id and user_id = @UserId and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<NotificationChannelRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        return row?.ToNotificationChannel();
    }

    public async Task<NotificationChannel?> GetActiveByTypeAndDestinationAsync(
        Guid userId,
        NotificationChannelType type,
        string destination,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, type, name, destination, is_active as IsActive,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from notification_channels
            where user_id = @UserId
              and type = @Type
              and destination = @Destination
              and is_active = true
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<NotificationChannelRow>(
            new CommandDefinition(
                sql,
                new { UserId = userId, Type = type.ToString(), Destination = destination },
                cancellationToken: cancellationToken));

        return row?.ToNotificationChannel();
    }

    public async Task AddAsync(NotificationChannel entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into notification_channels (
                id, user_id, type, name, destination, is_active, created_at_utc)
            values (
                @Id, @UserId, @Type, @Name, @Destination, @IsActive, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                entity.Id,
                entity.UserId,
                Type = entity.Type.ToString(),
                entity.Name,
                entity.Destination,
                entity.IsActive,
                entity.CreatedAtUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(NotificationChannel entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update notification_channels
            set type = @Type,
                name = @Name,
                destination = @Destination,
                is_active = @IsActive,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id and user_id = @UserId
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                entity.Id,
                entity.UserId,
                Type = entity.Type.ToString(),
                entity.Name,
                entity.Destination,
                entity.IsActive,
                entity.UpdatedAtUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update notification_channels
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

    private sealed record NotificationChannelRow(
        Guid Id,
        Guid UserId,
        string Type,
        string Name,
        string Destination,
        bool IsActive,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public NotificationChannel ToNotificationChannel()
        {
            return NotificationChannel.Restore(
                Id,
                UserId,
                Enum.Parse<NotificationChannelType>(Type),
                Name,
                Destination,
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
            "type" => "type",
            "destination" => "destination",
            "createdat" => "created_at_utc",
            "updatedat" => "updated_at_utc",
            "isactive" => "is_active",
            _ => "created_at_utc"
        };
    }
}
