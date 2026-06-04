using FluentAssertions;
using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Products;
using PriceWise.Application.Products.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Products;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesActiveProduct()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductService(repository, new NoOpDashboardCacheInvalidator(), new NoOpApplicationTelemetry());
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(userId, CreateRequest("https://example.com/product/1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.ProductUrl.Should().Be("https://example.com/product/1");
        repository.Products.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsyncFailsWhenProductUrlAlreadyExistsForSameUser()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductService(repository, new NoOpDashboardCacheInvalidator(), new NoOpApplicationTelemetry());
        var userId = Guid.NewGuid();
        await repository.AddAsync(Product.Create(
            userId,
            "Notebook",
            null,
            null,
            null,
            "https://example.com/product/1",
            null));

        var result = await service.CreateAsync(userId, CreateRequest("https://example.com/product/1"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.ProductUrlAlreadyRegistered);
    }

    [Fact]
    public async Task GetByIdAsyncDoesNotReturnProductFromAnotherUser()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductService(repository, new NoOpDashboardCacheInvalidator(), new NoOpApplicationTelemetry());
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var product = Product.Create(
            ownerId,
            "Notebook",
            null,
            null,
            null,
            "https://example.com/product/1",
            null);
        await repository.AddAsync(product);

        var result = await service.GetByIdAsync(anotherUserId, product.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.ProductNotFound);
    }

    [Fact]
    public async Task DeleteAsyncDeactivatesProduct()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductService(repository, new NoOpDashboardCacheInvalidator(), new NoOpApplicationTelemetry());
        var userId = Guid.NewGuid();
        var product = Product.Create(
            userId,
            "Notebook",
            null,
            null,
            null,
            "https://example.com/product/1",
            null);
        await repository.AddAsync(product);

        var result = await service.DeleteAsync(userId, product.Id);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
    }

    private static CreateProductRequest CreateRequest(string productUrl)
    {
        return new CreateProductRequest(
            "Notebook",
            "Notebook gamer",
            "Dell",
            "Eletronicos",
            productUrl,
            "https://example.com/image.png");
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];

        public Task<IReadOnlyCollection<Product>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Product> products = Products
                .Where(product => product.UserId == userId && product.IsActive)
                .ToArray();

            return Task.FromResult(products);
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Products.SingleOrDefault(product => product.Id == id && product.IsActive));
        }

        public Task<Product?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Products.SingleOrDefault(product =>
                product.Id == id && product.UserId == userId && product.IsActive));
        }

        public Task<Product?> GetByProductUrlAsync(
            Guid userId,
            string productUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Products.SingleOrDefault(product =>
                product.UserId == userId
                && product.ProductUrl == productUrl
                && product.IsActive));
        }

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            Products.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Products.RemoveAll(product => product.Id == id);

            return Task.CompletedTask;
        }
    }
}
