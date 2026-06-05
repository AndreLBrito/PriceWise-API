using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;
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
        var (statusCode, code, message) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation.InvalidRequest",
                "A requisição possui dados inválidos."),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Auth.Unauthorized",
                "Usuário não autenticado."),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource.NotFound",
                "Recurso não encontrado."),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Resource.Conflict",
                "A operação não pode ser concluída no estado atual."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "InternalServerError",
                "Ocorreu um erro inesperado.")
        };

        logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(code, message, statusCode);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
