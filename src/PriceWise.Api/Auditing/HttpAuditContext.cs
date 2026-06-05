using PriceWise.Application.Abstractions.Auditing;
using PriceWise.Application.Auditing;
using PriceWise.Api.Telemetry;

namespace PriceWise.Api.Auditing;

public sealed class HttpAuditContext : IAuditContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpAuditContext(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public AuditContextData GetCurrent()
    {
        var context = httpContextAccessor.HttpContext;

        return new AuditContextData(
            context?.Connection.RemoteIpAddress?.ToString(),
            context?.Request.Headers.UserAgent.ToString(),
            CorrelationContext.CorrelationId);
    }
}
