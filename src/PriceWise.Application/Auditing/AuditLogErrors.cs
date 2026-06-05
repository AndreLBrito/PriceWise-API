using PriceWise.Application.Common;

namespace PriceWise.Application.Auditing;

public static class AuditLogErrors
{
    public static readonly Error AuditLogNotFound = new(
        "AuditLogs.NotFound",
        "Registro de auditoria não encontrado.");
}
