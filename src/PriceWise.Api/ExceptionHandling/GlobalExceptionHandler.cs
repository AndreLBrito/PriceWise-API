using Microsoft.AspNetCore.Diagnostics;
using PriceWise.Api.Common;

namespace PriceWise.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            "InternalServerError",
            "An unexpected error occurred.");

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
