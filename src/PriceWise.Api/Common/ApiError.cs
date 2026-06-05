using System.Diagnostics;
using PriceWise.Api.Telemetry;

namespace PriceWise.Api.Common;

public sealed record ApiError(
    string Code,
    string Message,
    string? TraceId = null,
    string? CorrelationId = null,
    int? StatusCode = null)
{
    public static ApiError Create(string code, string message, int? statusCode = null)
    {
        return new ApiError(
            code,
            message,
            Activity.Current?.Id,
            CorrelationContext.CorrelationId,
            statusCode);
    }
}
