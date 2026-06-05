using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public AuditLogRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into audit_logs (
                id, user_id, action, entity_name, entity_id, old_values, new_values,
                ip_address, user_agent, correlation_id, created_at_utc)
            values (
                @Id, @UserId, @Action, @EntityName, @EntityId, @OldValues, @NewValues,
                @IpAddress, @UserAgent, @CorrelationId, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, auditLog, cancellationToken: cancellationToken));
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, action, entity_name as EntityName, entity_id as EntityId,
                   old_values as OldValues, new_values as NewValues, ip_address as IpAddress,
                   user_agent as UserAgent, correlation_id as CorrelationId, created_at_utc as CreatedAtUtc
            from audit_logs
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AuditLogRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToAuditLog();
    }

    public async Task<PagedResponse<AuditLog>> ListAsync(
        AuditLogListRequest request,
        CancellationToken cancellationToken = default)
    {
        var listRequest = request.ToListRequest();
        var sortColumn = GetSortColumn(request.SortBy);
        var sortDirection = listRequest.IsDescending ? "desc" : "asc";
        var whereSql = """
            where (@UserId is null or user_id = @UserId)
              and (@Action is null or action = @Action)
              and (@EntityName is null or entity_name = @EntityName)
              and (@EntityId is null or entity_id = @EntityId)
              and (@StartDate is null or created_at_utc >= @StartDate)
              and (@EndDate is null or created_at_utc <= @EndDate)
              and (
                  @Search is null
                  or action ilike @Search
                  or entity_name ilike @Search
                  or coalesce(correlation_id, '') ilike @Search
              )
            """;
        var countSql = $"select count(*) from audit_logs {whereSql}";
        var listSql = $"""
            select id, user_id as UserId, action, entity_name as EntityName, entity_id as EntityId,
                   old_values as OldValues, new_values as NewValues, ip_address as IpAddress,
                   user_agent as UserAgent, correlation_id as CorrelationId, created_at_utc as CreatedAtUtc
            from audit_logs
            {whereSql}
            order by {sortColumn} {sortDirection}
            limit @PageSize offset @Offset
            """;
        var parameters = new
        {
            request.UserId,
            Action = string.IsNullOrWhiteSpace(request.Action) ? null : request.Action.Trim(),
            EntityName = string.IsNullOrWhiteSpace(request.EntityName) ? null : request.EntityName.Trim(),
            request.EntityId,
            request.StartDate,
            request.EndDate,
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%",
            PageSize = listRequest.NormalizedPageSize,
            listRequest.Offset
        };

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var totalItems = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<AuditLogRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return PagedResponse<AuditLog>.Create(
            rows.Select(row => row.ToAuditLog()).ToArray(),
            listRequest.NormalizedPage,
            listRequest.NormalizedPageSize,
            totalItems);
    }

    private static string GetSortColumn(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "action" => "action",
            "entityname" => "entity_name",
            "createdat" => "created_at_utc",
            "correlationid" => "correlation_id",
            _ => "created_at_utc"
        };
    }

    private sealed record AuditLogRow(
        Guid Id,
        Guid? UserId,
        string Action,
        string EntityName,
        Guid? EntityId,
        string? OldValues,
        string? NewValues,
        string? IpAddress,
        string? UserAgent,
        string? CorrelationId,
        DateTime CreatedAtUtc)
    {
        public AuditLog ToAuditLog()
        {
            return AuditLog.Restore(
                Id,
                UserId,
                Action,
                EntityName,
                EntityId,
                OldValues,
                NewValues,
                IpAddress,
                UserAgent,
                CorrelationId,
                CreatedAtUtc);
        }
    }
}
