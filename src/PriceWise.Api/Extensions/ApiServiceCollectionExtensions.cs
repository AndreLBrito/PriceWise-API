using PriceWise.Api.ExceptionHandling;
using PriceWise.Api.Auditing;
using PriceWise.Application.Abstractions.Auditing;

namespace PriceWise.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditContext, HttpAuditContext>();

        return services;
    }
}
