using PriceWise.Application.Auditing;

namespace PriceWise.Application.Abstractions.Auditing;

public interface IAuditContext
{
    AuditContextData GetCurrent();
}
