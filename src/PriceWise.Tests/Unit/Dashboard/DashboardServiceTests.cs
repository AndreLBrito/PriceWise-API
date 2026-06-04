using FluentAssertions;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Dashboard;
using PriceWise.Application.Dashboard.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Dashboard;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsyncReturnsRepositorySummary()
    {
        var dashboardRepository = new InMemoryDashboardRepository();
        var service = new DashboardService(
            dashboardRepository,
            new InMemoryProductRepository(),
            new InMemoryStoreRepository());
        var userId = Guid.NewGuid();

        var result = await service.GetSummaryAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProducts.Should().Be(2);
        result.Value.ActiveProducts.Should().Be(1);
        dashboardRepository.LastUserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetProductPriceSummaryAsyncFailsWhenProductDoesNotBelongToUser()
    {
        var productRepository = new InMemoryProductRepository();
        var service = new DashboardService(
            new InMemoryDashboardRepository(),
            productRepository,
            new InMemoryStoreRepository());
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var result = await service.GetProductPriceSummaryAsync(userId, productId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DashboardErrors.ProductNotFound);
    }

    [Fact]
    public async Task GetProductPriceSummaryAsyncReturnsSummaryForOwnedProduct()
    {
        var dashboardRepository = new InMemoryDashboardRepository();
        var productRepository = new InMemoryProductRepository();
        var service = new DashboardService(
            dashboardRepository,
            productRepository,
            new InMemoryStoreRepository());
        var userId = Guid.NewGuid();
        var product = Product.Create(
            userId,
            "Notebook",
            null,
            null,
            null,
            "https://example.com/product/1",
            null);
        await productRepository.AddAsync(product);

        var result = await service.GetProductPriceSummaryAsync(userId, product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProductId.Should().Be(product.Id);
        result.Value.ProductName.Should().Be("Notebook");
        result.Value.LatestPrice.Should().Be(1299.90m);
        dashboardRepository.LastProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetStorePriceSummaryAsyncFailsWhenStoreDoesNotBelongToUser()
    {
        var service = new DashboardService(
            new InMemoryDashboardRepository(),
            new InMemoryProductRepository(),
            new InMemoryStoreRepository());
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var result = await service.GetStorePriceSummaryAsync(userId, storeId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DashboardErrors.StoreNotFound);
    }

    [Fact]
    public async Task GetStorePriceSummaryAsyncReturnsSummaryForOwnedStore()
    {
        var dashboardRepository = new InMemoryDashboardRepository();
        var storeRepository = new InMemoryStoreRepository();
        var service = new DashboardService(
            dashboardRepository,
            new InMemoryProductRepository(),
            storeRepository);
        var userId = Guid.NewGuid();
        var store = Store.Create(userId, "Store", "https://store.example.com", null);
        await storeRepository.AddAsync(store);

        var result = await service.GetStorePriceSummaryAsync(userId, store.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.StoreId.Should().Be(store.Id);
        result.Value.StoreName.Should().Be("Store");
        result.Value.TotalProductsMonitored.Should().Be(3);
        dashboardRepository.LastStoreId.Should().Be(store.Id);
    }

    [Fact]
    public async Task GetAlertSummaryAsyncReturnsRepositorySummary()
    {
        var dashboardRepository = new InMemoryDashboardRepository();
        var service = new DashboardService(
            dashboardRepository,
            new InMemoryProductRepository(),
            new InMemoryStoreRepository());
        var userId = Guid.NewGuid();

        var result = await service.GetAlertSummaryAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAlerts.Should().Be(4);
        result.Value.NotificationsLastSevenDays.Should().Be(2);
        dashboardRepository.LastUserId.Should().Be(userId);
    }

    private sealed class InMemoryDashboardRepository : IDashboardRepository
    {
        public Guid? LastUserId { get; private set; }

        public Guid? LastProductId { get; private set; }

        public Guid? LastStoreId { get; private set; }

        public Task<DashboardSummaryResponse> GetSummaryAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;

            return Task.FromResult(new DashboardSummaryResponse(
                2,
                1,
                1,
                3,
                2,
                1,
                10,
                4,
                3,
                5,
                49.90m,
                1299.90m,
                550.25m,
                DateTime.UtcNow));
        }

        public Task<ProductPriceSummaryResponse> GetProductPriceSummaryAsync(
            Guid userId,
            Guid productId,
            string productName,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastProductId = productId;

            return Task.FromResult(new ProductPriceSummaryResponse(
                productId,
                productName,
                5,
                999.90m,
                1499.90m,
                1199.90m,
                1299.90m,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(-10),
                30.01m,
                true,
                1100m));
        }

        public Task<StorePriceSummaryResponse> GetStorePriceSummaryAsync(
            Guid userId,
            Guid storeId,
            string storeName,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastStoreId = storeId;

            return Task.FromResult(new StorePriceSummaryResponse(
                storeId,
                storeName,
                3,
                7,
                19.90m,
                999.90m,
                320.25m,
                DateTime.UtcNow));
        }

        public Task<AlertSummaryResponse> GetAlertSummaryAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;

            return Task.FromResult(new AlertSummaryResponse(
                4,
                3,
                1,
                6,
                2,
                5,
                DateTime.UtcNow));
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> products = [];

        public Task<IReadOnlyCollection<Product>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Product> userProducts = products
                .Where(product => product.UserId == userId && product.IsActive)
                .ToArray();

            return Task.FromResult(userProducts);
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.SingleOrDefault(product => product.Id == id && product.IsActive));
        }

        public Task<Product?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.SingleOrDefault(product =>
                product.Id == id && product.UserId == userId && product.IsActive));
        }

        public Task<Product?> GetByProductUrlAsync(
            Guid userId,
            string productUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.SingleOrDefault(product =>
                product.UserId == userId
                && product.ProductUrl == productUrl
                && product.IsActive));
        }

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            products.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            products.RemoveAll(product => product.Id == id);

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryStoreRepository : IStoreRepository
    {
        private readonly List<Store> stores = [];

        public Task<IReadOnlyCollection<Store>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Store> userStores = stores
                .Where(store => store.UserId == userId && store.IsActive)
                .ToArray();

            return Task.FromResult(userStores);
        }

        public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stores.SingleOrDefault(store => store.Id == id && store.IsActive));
        }

        public Task<Store?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stores.SingleOrDefault(store =>
                store.Id == id && store.UserId == userId && store.IsActive));
        }

        public Task<Store?> GetByBaseUrlAsync(
            Guid userId,
            string baseUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stores.SingleOrDefault(store =>
                store.UserId == userId
                && store.BaseUrl == baseUrl
                && store.IsActive));
        }

        public Task AddAsync(Store entity, CancellationToken cancellationToken = default)
        {
            stores.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Store entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            stores.RemoveAll(store => store.Id == id);

            return Task.CompletedTask;
        }
    }
}
