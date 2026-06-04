using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Infrastructure.Caching;

namespace PriceWise.Infrastructure.DependencyInjection;

public static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddCacheInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = ReadOptions(configuration);

        services.Configure<CacheOptions>(cacheOptions =>
        {
            cacheOptions.Enabled = options.Enabled;
            cacheOptions.ConnectionString = options.ConnectionString;
            cacheOptions.DefaultExpirationInMinutes = options.DefaultExpirationInMinutes;
        });

        if (!options.Enabled)
        {
            services.AddSingleton<ICacheService, NoOpCacheService>();
            return services;
        }

        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = options.ConnectionString;
            redisOptions.InstanceName = "PriceWise:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static CacheOptions ReadOptions(IConfiguration configuration)
    {
        return new CacheOptions
        {
            Enabled = ReadBool(configuration, $"{CacheOptions.SectionName}:Enabled", true),
            ConnectionString = configuration[$"{CacheOptions.SectionName}:ConnectionString"] ?? "localhost:6379",
            DefaultExpirationInMinutes = ReadInt(
                configuration,
                $"{CacheOptions.SectionName}:DefaultExpirationInMinutes",
                10)
        };
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        return bool.TryParse(configuration[key], out var value) ? value : defaultValue;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        return int.TryParse(configuration[key], out var value) ? value : defaultValue;
    }
}
