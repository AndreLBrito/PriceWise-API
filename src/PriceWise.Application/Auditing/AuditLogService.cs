using System.Text.Json;
using System.Text.Json.Nodes;
using PriceWise.Application.Abstractions.Auditing;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Auditing;

public sealed class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] SensitiveTerms =
    [
        "password",
        "passwordhash",
        "refreshtoken",
        "token"
    ];

    private readonly IAuditLogRepository auditLogRepository;
    private readonly IAuditContext auditContext;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        IAuditContext auditContext)
    {
        this.auditLogRepository = auditLogRepository;
        this.auditContext = auditContext;
    }

    public async Task RecordAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        var context = auditContext.GetCurrent();
        var auditLog = AuditLog.Create(
            entry.UserId,
            entry.Action,
            entry.EntityName,
            entry.EntityId,
            SerializeSanitized(entry.OldValues),
            SerializeSanitized(entry.NewValues),
            context.IpAddress,
            context.UserAgent,
            context.CorrelationId);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    public async Task<Result<PagedResponse<AuditLogResponse>>> ListAsync(
        AuditLogListRequest request,
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await auditLogRepository.ListAsync(request, cancellationToken);
        var response = PagedResponse<AuditLogResponse>.Create(
            auditLogs.Items.Select(MapToResponse).ToArray(),
            auditLogs.Page,
            auditLogs.PageSize,
            auditLogs.TotalItems);

        return Result<PagedResponse<AuditLogResponse>>.Success(response);
    }

    public async Task<Result<AuditLogResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await auditLogRepository.GetByIdAsync(id, cancellationToken);

        return auditLog is null
            ? Result<AuditLogResponse>.Failure(AuditLogErrors.AuditLogNotFound)
            : Result<AuditLogResponse>.Success(MapToResponse(auditLog));
    }

    private static string? SerializeSanitized(object? values)
    {
        if (values is null)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(values, JsonOptions);
        Sanitize(node);

        return node?.ToJsonString(JsonOptions);
    }

    private static void Sanitize(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                if (IsSensitive(property.Key))
                {
                    jsonObject.Remove(property.Key);
                    continue;
                }

                Sanitize(property.Value);
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                Sanitize(item);
            }
        }
    }

    private static bool IsSensitive(string propertyName)
    {
        var normalizedName = propertyName.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return SensitiveTerms.Any(normalizedName.Contains);
    }

    private static AuditLogResponse MapToResponse(AuditLog auditLog)
    {
        return new AuditLogResponse(
            auditLog.Id,
            auditLog.UserId,
            auditLog.Action,
            auditLog.EntityName,
            auditLog.EntityId,
            auditLog.OldValues,
            auditLog.NewValues,
            auditLog.IpAddress,
            auditLog.UserAgent,
            auditLog.CorrelationId,
            auditLog.CreatedAtUtc);
    }
}
