using PriceWise.Api.Cors;

namespace PriceWise.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddPriceWiseCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(ApiCorsOptions.SectionName);
        var options = section.Get<ApiCorsOptions>() ?? new ApiCorsOptions();

        services.Configure<ApiCorsOptions>(section);
        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(ApiCorsPolicyNames.Development, policy =>
            {
                if (options.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(options.AllowedOrigins);
                }

                if (options.AllowedMethods.Length > 0)
                {
                    policy.WithMethods(options.AllowedMethods);
                }

                if (options.AllowedHeaders.Length > 0)
                {
                    policy.WithHeaders(options.AllowedHeaders);
                }
            });
        });

        return services;
    }
}
