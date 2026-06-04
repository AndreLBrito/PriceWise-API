using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Telemetry;

namespace PriceWise.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache distributedCache;
    private readonly CacheOptions options;
    private readonly ILogger<RedisCacheService> logger;
    private readonly IApplicationTelemetry telemetry;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger,
        IApplicationTelemetry telemetry)
    {
        this.distributedCache = distributedCache;
        this.options = options.Value;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return default;
        }

        try
        {
            using var activity = telemetry.StartActivity("Redis.Get");
            var value = await distributedCache.GetStringAsync(key, cancellationToken);

            return string.IsNullOrWhiteSpace(value)
                ? default
                : JsonSerializer.Deserialize<T>(value, JsonOptions);
        }
        catch (Exception exception)
        {
            telemetry.RecordError(exception);
            logger.LogWarning(exception, "Falha ao ler cache Redis para a chave {CacheKey}. A API seguirá sem cache.", key);

            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return;
        }

        try
        {
            using var activity = telemetry.StartActivity("Redis.Set");
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? GetDefaultExpiration()
            };
            var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

            await distributedCache.SetStringAsync(key, serializedValue, cacheOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            telemetry.RecordError(exception);
            logger.LogWarning(exception, "Falha ao gravar cache Redis para a chave {CacheKey}. A API seguirá sem cache.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return;
        }

        try
        {
            using var activity = telemetry.StartActivity("Redis.Remove");
            await distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception exception)
        {
            telemetry.RecordError(exception);
            logger.LogWarning(exception, "Falha ao remover cache Redis para a chave {CacheKey}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return;
        }

        await RemoveAsync(prefix, cancellationToken);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return await factory(cancellationToken);
        }

        var cachedValue = await GetAsync<T>(key, cancellationToken);

        if (cachedValue is not null)
        {
            return cachedValue;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);

        return value;
    }

    private TimeSpan GetDefaultExpiration()
    {
        return TimeSpan.FromMinutes(Math.Max(1, options.DefaultExpirationInMinutes));
    }
}
