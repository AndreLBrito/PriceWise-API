using FluentAssertions;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.PriceAlerts;
using PriceWise.Application.PriceAlerts.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.PriceAlerts;

public sealed class PriceAlertServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesActivePriceAlert()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var repository = new InMemoryPriceAlertRepository();
        var service = CreateService(repository, new InMemoryProductRepository(product));

        var result = await service.CreateAsync(userId, new CreatePriceAlertRequest(product.Id, 100));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.TargetPrice.Should().Be(100);
        repository.PriceAlerts.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsyncFailsWhenProductDoesNotBelongToUser()
    {
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var product = Product.Create(ownerId, "Notebook", null, null, null, "https://example.com/p", null);
        var service = CreateService(
            new InMemoryPriceAlertRepository(),
            new InMemoryProductRepository(product));

        var result = await service.CreateAsync(anotherUserId, new CreatePriceAlertRequest(product.Id, 100));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceAlertErrors.ProductNotFound);
    }

    [Fact]
    public async Task CreateAsyncFailsWhenActiveAlertAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var repository = new InMemoryPriceAlertRepository();
        await repository.AddAsync(PriceAlert.Create(userId, product.Id, 100));
        var service = CreateService(repository, new InMemoryProductRepository(product));

        var result = await service.CreateAsync(userId, new CreatePriceAlertRequest(product.Id, 90));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceAlertErrors.ActiveAlertAlreadyExists);
    }

    [Fact]
    public async Task UpdateAsyncUpdatesTargetPrice()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var alert = PriceAlert.Create(userId, product.Id, 100);
        var repository = new InMemoryPriceAlertRepository();
        await repository.AddAsync(alert);
        var service = CreateService(repository, new InMemoryProductRepository(product));

        var result = await service.UpdateAsync(userId, alert.Id, new UpdatePriceAlertRequest(80));

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetPrice.Should().Be(80);
    }

    [Fact]
    public async Task DeleteAsyncDeactivatesPriceAlert()
    {
        var userId = Guid.NewGuid();
        var product = Product.Create(userId, "Notebook", null, null, null, "https://example.com/p", null);
        var alert = PriceAlert.Create(userId, product.Id, 100);
        var repository = new InMemoryPriceAlertRepository();
        await repository.AddAsync(alert);
        var service = CreateService(repository, new InMemoryProductRepository(product));

        var result = await service.DeleteAsync(userId, alert.Id);

        result.IsSuccess.Should().BeTrue();
        alert.IsActive.Should().BeFalse();
    }

    private static PriceAlertService CreateService(
        IPriceAlertRepository priceAlertRepository,
        IProductRepository productRepository)
    {
        return new PriceAlertService(
            priceAlertRepository,
            productRepository,
            new NoOpDashboardCacheInvalidator(),
            new NoOpApplicationTelemetry());
    }

    private sealed class InMemoryPriceAlertRepository : IPriceAlertRepository
    {
        public List<PriceAlert> PriceAlerts { get; } = [];

        public Task<IReadOnlyCollection<PriceAlert>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PriceAlert> result = PriceAlerts
                .Where(priceAlert => priceAlert.UserId == userId && priceAlert.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<PriceAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PriceAlerts.SingleOrDefault(priceAlert => priceAlert.Id == id && priceAlert.IsActive));
        }

        public Task<PriceAlert?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PriceAlerts.SingleOrDefault(priceAlert =>
                priceAlert.Id == id && priceAlert.UserId == userId && priceAlert.IsActive));
        }

        public Task<PriceAlert?> GetActiveByProductIdAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PriceAlerts.SingleOrDefault(priceAlert =>
                priceAlert.UserId == userId
                && priceAlert.ProductId == productId
                && priceAlert.IsActive));
        }

        public Task<IReadOnlyCollection<PriceAlert>> ListActiveByProductIdAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PriceAlert> result = PriceAlerts
                .Where(priceAlert => priceAlert.UserId == userId
                    && priceAlert.ProductId == productId
                    && priceAlert.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task AddAsync(PriceAlert entity, CancellationToken cancellationToken = default)
        {
            PriceAlerts.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(PriceAlert entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            PriceAlerts.RemoveAll(priceAlert => priceAlert.Id == id);

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> products;

        public InMemoryProductRepository(params Product[] products)
        {
            this.products = products.ToList();
        }

        public Task<IReadOnlyCollection<Product>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Product> result = products
                .Where(product => product.UserId == userId && product.IsActive)
                .ToArray();

            return Task.FromResult(result);
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
            return Task.CompletedTask;
        }
    }
}
