using PriceWise.Application.AlertNotifications;
using FluentAssertions;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.PriceHistories;
using PriceWise.Application.PriceHistories.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.PriceHistories;

public sealed class PriceHistoryServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesPriceHistoryWhenProductAndStoreBelongToUser()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var store = Store.Create(userId, "Loja", "https://example.com", null);
        var priceHistoryRepository = new InMemoryPriceHistoryRepository();
        var service = CreateService(
            new InMemoryProductRepository(product),
            new InMemoryStoreRepository(store),
            priceHistoryRepository);

        var result = await service.CreateAsync(userId, CreateRequest(product.Id, store.Id, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("BRL");
        result.Value.CapturedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        priceHistoryRepository.PriceHistories.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsyncFailsWhenProductDoesNotBelongToUser()
    {
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var product = Product.Create(ownerId, "Notebook", null, null, null, "https://example.com/p", null);
        var store = Store.Create(anotherUserId, "Loja", "https://example.com", null);
        var service = CreateService(
            new InMemoryProductRepository(product),
            new InMemoryStoreRepository(store),
            new InMemoryPriceHistoryRepository());

        var result = await service.CreateAsync(anotherUserId, CreateRequest(product.Id, store.Id, DateTime.UtcNow));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceHistoryErrors.ProductNotFound);
    }

    [Fact]
    public async Task CreateAsyncFailsWhenStoreDoesNotBelongToUser()
    {
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var product = Product.Create(ownerId, "Notebook", null, null, null, "https://example.com/p", null);
        var store = Store.Create(anotherUserId, "Loja", "https://example.com", null);
        var service = CreateService(
            new InMemoryProductRepository(product),
            new InMemoryStoreRepository(store),
            new InMemoryPriceHistoryRepository());

        var result = await service.CreateAsync(ownerId, CreateRequest(product.Id, store.Id, DateTime.UtcNow));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceHistoryErrors.StoreNotFound);
    }

    [Fact]
    public async Task GetLowestAsyncReturnsLowestPrice()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var store = Store.Create(userId, "Loja", "https://example.com", null);
        var priceHistoryRepository = new InMemoryPriceHistoryRepository();
        await priceHistoryRepository.AddAsync(PriceHistory.Create(userId, product.Id, store.Id, 100, "BRL", DateTime.UtcNow.AddDays(-1), null));
        await priceHistoryRepository.AddAsync(PriceHistory.Create(userId, product.Id, store.Id, 80, "BRL", DateTime.UtcNow, null));
        var service = CreateService(
            new InMemoryProductRepository(product),
            new InMemoryStoreRepository(store),
            priceHistoryRepository);

        var result = await service.GetLowestAsync(userId, product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Price.Should().Be(80);
    }

    [Fact]
    public async Task GetAverageAsyncReturnsAveragePrice()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var store = Store.Create(userId, "Loja", "https://example.com", null);
        var priceHistoryRepository = new InMemoryPriceHistoryRepository();
        await priceHistoryRepository.AddAsync(PriceHistory.Create(userId, product.Id, store.Id, 100, "BRL", DateTime.UtcNow.AddDays(-1), null));
        await priceHistoryRepository.AddAsync(PriceHistory.Create(userId, product.Id, store.Id, 80, "BRL", DateTime.UtcNow, null));
        var service = CreateService(
            new InMemoryProductRepository(product),
            new InMemoryStoreRepository(store),
            priceHistoryRepository);

        var result = await service.GetAverageAsync(userId, product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.AveragePrice.Should().Be(90);
        result.Value.EntriesCount.Should().Be(2);
    }

    private static PriceHistoryService CreateService(
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IPriceHistoryRepository priceHistoryRepository)
    {
        return new PriceHistoryService(
            priceHistoryRepository,
            productRepository,
            storeRepository,
            new NoOpAlertNotificationService());
    }

    private static CreatePriceHistoryRequest CreateRequest(Guid productId, Guid storeId, DateTime? capturedAt)
    {
        return new CreatePriceHistoryRequest(
            productId,
            storeId,
            99.90m,
            "brl",
            capturedAt,
            "https://example.com/source");
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> products;

        public InMemoryProductRepository(params Product[] products)
        {
            this.products = products.ToList();
        }

        public Task<IReadOnlyCollection<Product>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Product> result = products.Where(product => product.UserId == userId && product.IsActive).ToArray();
            return Task.FromResult(result);
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.SingleOrDefault(product => product.Id == id && product.IsActive));
        }

        public Task<Product?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.SingleOrDefault(product => product.Id == id && product.UserId == userId && product.IsActive));
        }

        public Task<Product?> GetByProductUrlAsync(Guid userId, string productUrl, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.SingleOrDefault(product => product.UserId == userId && product.ProductUrl == productUrl && product.IsActive));
        }

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            products.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryStoreRepository : IStoreRepository
    {
        private readonly List<Store> stores;

        public InMemoryStoreRepository(params Store[] stores)
        {
            this.stores = stores.ToList();
        }

        public Task<IReadOnlyCollection<Store>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Store> result = stores.Where(store => store.UserId == userId && store.IsActive).ToArray();
            return Task.FromResult(result);
        }

        public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stores.SingleOrDefault(store => store.Id == id && store.IsActive));
        }

        public Task<Store?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stores.SingleOrDefault(store => store.Id == id && store.UserId == userId && store.IsActive));
        }

        public Task<Store?> GetByBaseUrlAsync(Guid userId, string baseUrl, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stores.SingleOrDefault(store => store.UserId == userId && store.BaseUrl == baseUrl && store.IsActive));
        }

        public Task AddAsync(Store entity, CancellationToken cancellationToken = default)
        {
            stores.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Store entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryPriceHistoryRepository : IPriceHistoryRepository
    {
        public List<PriceHistory> PriceHistories { get; } = [];

        public Task<IReadOnlyCollection<PriceHistory>> ListByProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PriceHistory> result = PriceHistories
                .Where(priceHistory => priceHistory.UserId == userId && priceHistory.ProductId == productId)
                .OrderByDescending(priceHistory => priceHistory.CapturedAt)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<PriceHistory?> GetLatestAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PriceHistories
                .Where(priceHistory => priceHistory.UserId == userId && priceHistory.ProductId == productId)
                .OrderByDescending(priceHistory => priceHistory.CapturedAt)
                .FirstOrDefault());
        }

        public Task<PriceHistory?> GetLowestAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PriceHistories
                .Where(priceHistory => priceHistory.UserId == userId && priceHistory.ProductId == productId)
                .OrderBy(priceHistory => priceHistory.Price)
                .ThenByDescending(priceHistory => priceHistory.CapturedAt)
                .FirstOrDefault());
        }

        public Task<(decimal AveragePrice, int EntriesCount)?> GetAverageAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        {
            var entries = PriceHistories
                .Where(priceHistory => priceHistory.UserId == userId && priceHistory.ProductId == productId)
                .ToArray();

            return Task.FromResult(entries.Length == 0
                ? null
                : ((decimal AveragePrice, int EntriesCount)?)(entries.Average(priceHistory => priceHistory.Price), entries.Length));
        }

        public Task<PriceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PriceHistories.SingleOrDefault(priceHistory => priceHistory.Id == id));
        }

        public Task AddAsync(PriceHistory entity, CancellationToken cancellationToken = default)
        {
            PriceHistories.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PriceHistory entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpAlertNotificationService : IAlertNotificationService
    {
        public Task CheckPriceAlertsAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<PriceWise.Application.Common.Result<IReadOnlyCollection<PriceWise.Application.AlertNotifications.Dtos.AlertNotificationResponse>>> ListAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PriceWise.Application.Common.Result<PriceWise.Application.AlertNotifications.Dtos.AlertNotificationResponse>> GetByIdAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
