using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.Outbox.Dtos;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public OutboxRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> ListPendingAsync(
        int batchSize,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, type, payload, status, retry_count as RetryCount, max_retries as MaxRetries,
                   next_attempt_at as NextAttemptAt, processed_at as ProcessedAt,
                   error_message as ErrorMessage, correlation_id as CorrelationId,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from outbox_messages
            where status = @Status
              and next_attempt_at <= @UtcNow
            order by next_attempt_at, created_at_utc
            limit @BatchSize
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<OutboxMessageRow>(new CommandDefinition(
            sql,
            new { Status = OutboxMessageStatus.Pending.ToString(), UtcNow = utcNow, BatchSize = Math.Max(1, batchSize) },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToOutboxMessage()).ToArray();
    }

    public async Task<PagedResponse<OutboxMessage>> ListAsync(
        OutboxListRequest request,
        CancellationToken cancellationToken = default)
    {
        var listRequest = request.ToListRequest();
        var sortColumn = GetSortColumn(request.SortBy);
        var sortDirection = listRequest.IsDescending ? "desc" : "asc";
        var whereSql = """
            where (@Status is null or status = @Status)
              and (@Type is null or type = @Type)
              and (@StartDate is null or created_at_utc >= @StartDate)
              and (@EndDate is null or created_at_utc <= @EndDate)
              and (
                  @Search is null
                  or type ilike @Search
                  or status ilike @Search
                  or coalesce(error_message, '') ilike @Search
                  or coalesce(correlation_id, '') ilike @Search
              )
            """;
        var countSql = $"select count(*) from outbox_messages {whereSql}";
        var listSql = $"""
            select id, type, payload, status, retry_count as RetryCount, max_retries as MaxRetries,
                   next_attempt_at as NextAttemptAt, processed_at as ProcessedAt,
                   error_message as ErrorMessage, correlation_id as CorrelationId,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from outbox_messages
            {whereSql}
            order by {sortColumn} {sortDirection}
            limit @PageSize offset @Offset
            """;
        var parameters = new
        {
            Status = request.Status?.ToString(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? null : request.Type.Trim(),
            request.StartDate,
            request.EndDate,
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%",
            PageSize = listRequest.NormalizedPageSize,
            listRequest.Offset
        };

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var totalItems = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<OutboxMessageRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return PagedResponse<OutboxMessage>.Create(
            rows.Select(row => row.ToOutboxMessage()).ToArray(),
            listRequest.NormalizedPage,
            listRequest.NormalizedPageSize,
            totalItems);
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, type, payload, status, retry_count as RetryCount, max_retries as MaxRetries,
                   next_attempt_at as NextAttemptAt, processed_at as ProcessedAt,
                   error_message as ErrorMessage, correlation_id as CorrelationId,
                   created_at_utc as CreatedAtUtc, updated_at_utc as UpdatedAtUtc
            from outbox_messages
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OutboxMessageRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToOutboxMessage();
    }

    public async Task AddAsync(OutboxMessage entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into outbox_messages (
                id, type, payload, status, retry_count, max_retries, next_attempt_at,
                processed_at, error_message, correlation_id, created_at_utc, updated_at_utc)
            values (
                @Id, @Type, @Payload, @Status, @RetryCount, @MaxRetries, @NextAttemptAt,
                @ProcessedAt, @ErrorMessage, @CorrelationId, @CreatedAtUtc, @UpdatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            ToParameters(entity),
            cancellationToken: cancellationToken));
    }

    public Task UpdateAsync(OutboxMessage entity, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update outbox_messages
            set status = @Status,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, Status = OutboxMessageStatus.Processing.ToString(), UpdatedAtUtc = DateTime.UtcNow },
            cancellationToken: cancellationToken));
    }

    public async Task MarkProcessedAsync(
        Guid id,
        DateTime processedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update outbox_messages
            set status = @Status,
                processed_at = @ProcessedAt,
                error_message = null,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id,
                Status = OutboxMessageStatus.Processed.ToString(),
                ProcessedAt = processedAt,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    public async Task ScheduleRetryAsync(
        Guid id,
        int retryCount,
        OutboxMessageStatus status,
        DateTime nextAttemptAt,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update outbox_messages
            set status = @Status,
                retry_count = @RetryCount,
                next_attempt_at = @NextAttemptAt,
                error_message = @ErrorMessage,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id,
                Status = status.ToString(),
                RetryCount = retryCount,
                NextAttemptAt = nextAttemptAt,
                ErrorMessage = errorMessage,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    public async Task ResetFailedAsync(
        Guid id,
        DateTime nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update outbox_messages
            set status = @Status,
                next_attempt_at = @NextAttemptAt,
                processed_at = null,
                error_message = null,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id and status = @FailedStatus
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id,
                Status = OutboxMessageStatus.Pending.ToString(),
                FailedStatus = OutboxMessageStatus.Failed.ToString(),
                NextAttemptAt = nextAttemptAt,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    private static object ToParameters(OutboxMessage entity)
    {
        return new
        {
            entity.Id,
            entity.Type,
            entity.Payload,
            Status = entity.Status.ToString(),
            entity.RetryCount,
            entity.MaxRetries,
            entity.NextAttemptAt,
            entity.ProcessedAt,
            entity.ErrorMessage,
            entity.CorrelationId,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc
        };
    }

    private static string GetSortColumn(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "type" => "type",
            "status" => "status",
            "retrycount" => "retry_count",
            "nextattemptat" => "next_attempt_at",
            "processedat" => "processed_at",
            "createdat" => "created_at_utc",
            "updatedat" => "updated_at_utc",
            _ => "created_at_utc"
        };
    }

    private sealed record OutboxMessageRow(
        Guid Id,
        string Type,
        string Payload,
        string Status,
        int RetryCount,
        int MaxRetries,
        DateTime NextAttemptAt,
        DateTime? ProcessedAt,
        string? ErrorMessage,
        string? CorrelationId,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public OutboxMessage ToOutboxMessage()
        {
            return OutboxMessage.Restore(
                Id,
                Type,
                Payload,
                Enum.Parse<OutboxMessageStatus>(Status),
                RetryCount,
                MaxRetries,
                NextAttemptAt,
                ProcessedAt,
                ErrorMessage,
                CorrelationId,
                CreatedAtUtc,
                UpdatedAtUtc);
        }
    }
}
