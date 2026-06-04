using FluentAssertions;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Stores;
using PriceWise.Application.Stores.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Stores;

public sealed class StoreServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesActiveStore()
    {
        var repository = new InMemoryStoreRepository();
        var service = new StoreService(repository, new NoOpDashboardCacheInvalidator());
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(userId, CreateRequest("https://example.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.BaseUrl.Should().Be("https://example.com");
        repository.Stores.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsyncFailsWhenBaseUrlAlreadyExistsForSameUser()
    {
        var repository = new InMemoryStoreRepository();
        var service = new StoreService(repository, new NoOpDashboardCacheInvalidator());
        var userId = Guid.NewGuid();
        await repository.AddAsync(Store.Create(userId, "Loja", "https://example.com", null));

        var result = await service.CreateAsync(userId, CreateRequest("https://example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StoreErrors.BaseUrlAlreadyRegistered);
    }

    [Fact]
    public async Task GetByIdAsyncDoesNotReturnStoreFromAnotherUser()
    {
        var repository = new InMemoryStoreRepository();
        var service = new StoreService(repository, new NoOpDashboardCacheInvalidator());
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var store = Store.Create(ownerId, "Loja", "https://example.com", null);
        await repository.AddAsync(store);

        var result = await service.GetByIdAsync(anotherUserId, store.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StoreErrors.StoreNotFound);
    }

    [Fact]
    public async Task DeleteAsyncDeactivatesStore()
    {
        var repository = new InMemoryStoreRepository();
        var service = new StoreService(repository, new NoOpDashboardCacheInvalidator());
        var userId = Guid.NewGuid();
        var store = Store.Create(userId, "Loja", "https://example.com", null);
        await repository.AddAsync(store);

        var result = await service.DeleteAsync(userId, store.Id);

        result.IsSuccess.Should().BeTrue();
        store.IsActive.Should().BeFalse();
    }

    private static CreateStoreRequest CreateRequest(string baseUrl)
    {
        return new CreateStoreRequest("Loja", baseUrl, "https://example.com/logo.png");
    }

    private sealed class InMemoryStoreRepository : IStoreRepository
    {
        public List<Store> Stores { get; } = [];

        public Task<IReadOnlyCollection<Store>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Store> stores = Stores
                .Where(store => store.UserId == userId && store.IsActive)
                .ToArray();

            return Task.FromResult(stores);
        }

        public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stores.SingleOrDefault(store => store.Id == id && store.IsActive));
        }

        public Task<Store?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stores.SingleOrDefault(store =>
                store.Id == id && store.UserId == userId && store.IsActive));
        }

        public Task<Store?> GetByBaseUrlAsync(
            Guid userId,
            string baseUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stores.SingleOrDefault(store =>
                store.UserId == userId
                && store.BaseUrl == baseUrl
                && store.IsActive));
        }

        public Task AddAsync(Store entity, CancellationToken cancellationToken = default)
        {
            Stores.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Store entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Stores.RemoveAll(store => store.Id == id);

            return Task.CompletedTask;
        }
    }
}
