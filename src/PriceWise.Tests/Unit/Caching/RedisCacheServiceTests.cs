using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Infrastructure.Caching;

namespace PriceWise.Tests.Unit.Caching;

public sealed class RedisCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsyncReturnsValueFromCache()
    {
        var distributedCache = new InMemoryDistributedCache();
        var service = CreateService(distributedCache);
        await service.SetAsync("key", new CachedValue("cached"));
        var factoryWasCalled = false;

        var result = await service.GetOrCreateAsync("key", _ =>
        {
            factoryWasCalled = true;
            return Task.FromResult(new CachedValue("created"));
        });

        result.Name.Should().Be("cached");
        factoryWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsyncCreatesValueWhenCacheDoesNotExist()
    {
        var service = CreateService(new InMemoryDistributedCache());

        var result = await service.GetOrCreateAsync(
            "key",
            _ => Task.FromResult(new CachedValue("created")));

        result.Name.Should().Be("created");
        var cachedValue = await service.GetAsync<CachedValue>("key");
        cachedValue.Should().NotBeNull();
        cachedValue!.Name.Should().Be("created");
    }

    [Fact]
    public async Task GetOrCreateAsyncIgnoresRedisWhenCacheIsDisabled()
    {
        var distributedCache = new InMemoryDistributedCache();
        var service = CreateService(distributedCache, enabled: false);
        await service.SetAsync("key", new CachedValue("cached"));

        var result = await service.GetOrCreateAsync(
            "key",
            _ => Task.FromResult(new CachedValue("created")));

        result.Name.Should().Be("created");
    }

    [Fact]
    public async Task GetOrCreateAsyncDoesNotBreakWhenRedisFails()
    {
        var service = CreateService(new FailingDistributedCache());

        var result = await service.GetOrCreateAsync(
            "key",
            _ => Task.FromResult(new CachedValue("created")));

        result.Name.Should().Be("created");
    }

    [Fact]
    public void DashboardSummaryKeyContainsUserId()
    {
        var userId = Guid.NewGuid();

        var key = CacheKeys.DashboardSummary(userId);

        key.Should().Contain(userId.ToString("N"));
    }

    private static RedisCacheService CreateService(
        IDistributedCache distributedCache,
        bool enabled = true)
    {
        return new RedisCacheService(
            distributedCache,
            Options.Create(new CacheOptions
            {
                Enabled = enabled,
                ConnectionString = "localhost:6379",
                DefaultExpirationInMinutes = 10
            }),
            NullLogger<RedisCacheService>.Instance,
            new NoOpApplicationTelemetry());
    }

    private sealed record CachedValue(string Name);

    private sealed class InMemoryDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> values = [];

        public byte[]? Get(string key)
        {
            return values.GetValueOrDefault(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            values[key] = value;
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Set(key, value, options);

            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            values.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);

            return Task.CompletedTask;
        }
    }

    private sealed class FailingDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key)
        {
            throw new InvalidOperationException("Redis unavailable.");
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            throw new InvalidOperationException("Redis unavailable.");
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw new InvalidOperationException("Redis unavailable.");
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            throw new InvalidOperationException("Redis unavailable.");
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            throw new InvalidOperationException("Redis unavailable.");
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            throw new InvalidOperationException("Redis unavailable.");
        }
    }
}
