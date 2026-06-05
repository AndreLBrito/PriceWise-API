using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResponse<AuditLog>> ListAsync(
        AuditLogListRequest request,
        CancellationToken cancellationToken = default);
}
