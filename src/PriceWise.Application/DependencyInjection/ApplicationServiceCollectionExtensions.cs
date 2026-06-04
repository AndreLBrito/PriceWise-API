using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Services;

namespace PriceWise.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddScoped<IDashboardCacheInvalidator, DashboardCacheInvalidator>();
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var serviceTypes = typeof(ApplicationServiceCollectionExtensions)
            .Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IService).IsAssignableFrom(type));

        foreach (var implementationType in serviceTypes)
        {
            var interfaces = implementationType
                .GetInterfaces()
                .Where(type => type != typeof(IService) && typeof(IService).IsAssignableFrom(type));

            foreach (var serviceType in interfaces)
            {
                services.AddScoped(serviceType, implementationType);
            }
        }

        return services;
    }
}
