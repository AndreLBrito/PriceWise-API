using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;

namespace PriceWise.Application.Auditing;

public interface IAuditLogService : IService
{
    Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<AuditLogResponse>>> ListAsync(
        AuditLogListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuditLogResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
