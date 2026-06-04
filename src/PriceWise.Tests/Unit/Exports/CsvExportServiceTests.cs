using FluentAssertions;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Exports;
using PriceWise.Application.Exports.Dtos;

namespace PriceWise.Tests.Unit.Exports;

public sealed class CsvExportServiceTests
{
    [Fact]
    public async Task ExportProductsAsyncReturnsValidCsv()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryExportRepository();
        repository.Products.Add(new UserProductExportRow(
            userId,
            new ProductExportRow(
                Guid.NewGuid(),
                "Notebook",
                "Produto de teste",
                "Dell",
                "Notebook",
                "https://example.com/notebook",
                null,
                true,
                new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                null)));
        var service = CreateService(repository);

        var result = await service.ExportProductsAsync(userId, new ExportFilter());

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv; charset=utf-8");
        result.Value.FileName.Should().StartWith("pricewise-produtos-");
        result.Value.Content.Should().Contain("Id,Name,Description,Brand,Category,ProductUrl,ImageUrl,IsActive,CreatedAtUtc,UpdatedAtUtc");
        result.Value.Content.Should().Contain("Notebook");
        result.Value.Content.Should().Contain("2026-06-01 10:00:00");
    }

    [Fact]
    public async Task ExportPriceHistoriesAsyncUsesProductFilter()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var repository = new InMemoryExportRepository();
        repository.PriceHistories.Add(CreatePriceHistory(userId, productId, 90));
        repository.PriceHistories.Add(CreatePriceHistory(userId, otherProductId, 120));
        var service = CreateService(repository);

        var result = await service.ExportPriceHistoriesAsync(userId, new ExportFilter(ProductId: productId));

        result.Value.Content.Should().Contain(productId.ToString());
        result.Value.Content.Should().NotContain(otherProductId.ToString());
        repository.LastPriceHistoryFilter.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task ExportPriceHistoriesAsyncUsesDateFilter()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var startDate = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 6, 3, 23, 59, 59, DateTimeKind.Utc);
        var repository = new InMemoryExportRepository();
        repository.PriceHistories.Add(CreatePriceHistory(userId, productId, 100, new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)));
        repository.PriceHistories.Add(CreatePriceHistory(userId, productId, 90, new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc)));
        var service = CreateService(repository);

        var result = await service.ExportPriceHistoriesAsync(userId, new ExportFilter(startDate, endDate));

        result.Value.Content.Should().Contain("90");
        result.Value.Content.Should().NotContain("100");
        repository.LastPriceHistoryFilter.StartDate.Should().Be(startDate);
        repository.LastPriceHistoryFilter.EndDate.Should().Be(endDate);
    }

    [Fact]
    public async Task ExportProductsAsyncDoesNotReturnDataFromAnotherUser()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var repository = new InMemoryExportRepository();
        repository.Products.Add(new UserProductExportRow(userId, CreateProduct("Produto do usuário")));
        repository.Products.Add(new UserProductExportRow(otherUserId, CreateProduct("Produto de outro usuário")));
        var service = CreateService(repository);

        var result = await service.ExportProductsAsync(userId, new ExportFilter());

        result.Value.Content.Should().Contain("Produto do usuário");
        result.Value.Content.Should().NotContain("Produto de outro usuário");
    }

    [Fact]
    public async Task ExportProductsAsyncEscapesCommasQuotesAndLineBreaks()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryExportRepository();
        repository.Products.Add(new UserProductExportRow(
            userId,
            CreateProduct("Notebook, \"Pro\"\nLinha 2")));
        var service = CreateService(repository);

        var result = await service.ExportProductsAsync(userId, new ExportFilter());

        result.Value.Content.Should().Contain("\"Notebook, \"\"Pro\"\"");
        result.Value.Content.Should().Contain("Linha 2\"");
    }

    private static CsvExportService CreateService(InMemoryExportRepository repository)
    {
        return new CsvExportService(
            repository,
            Options.Create(new CsvExportOptions
            {
                MaxRows = 10_000,
                DateFormat = "yyyy-MM-dd HH:mm:ss"
            }));
    }

    private static ProductExportRow CreateProduct(string name)
    {
        return new ProductExportRow(
            Guid.NewGuid(),
            name,
            "Descrição",
            "Marca",
            "Categoria",
            "https://example.com/product",
            null,
            true,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            null);
    }

    private static UserPriceHistoryExportRow CreatePriceHistory(
        Guid userId,
        Guid productId,
        decimal price,
        DateTime? capturedAt = null)
    {
        return new UserPriceHistoryExportRow(
            userId,
            new PriceHistoryExportRow(
                Guid.NewGuid(),
                productId,
                "Produto",
                Guid.NewGuid(),
                "Loja",
                price,
                "BRL",
                capturedAt ?? new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc),
                "https://example.com/source",
                new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc)));
    }

    private sealed class InMemoryExportRepository : IExportRepository
    {
        public List<UserProductExportRow> Products { get; } = [];

        public List<UserPriceHistoryExportRow> PriceHistories { get; } = [];

        public ExportFilter LastPriceHistoryFilter { get; private set; } = new();

        public Task<IReadOnlyCollection<ProductExportRow>> ListProductsAsync(
            Guid userId,
            ExportFilter filter,
            int maxRows,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProductExportRow> rows = Products
                .Where(row => row.UserId == userId)
                .Where(row => filter.ProductId is null || row.Row.Id == filter.ProductId)
                .Where(row => filter.StartDate is null || row.Row.CreatedAtUtc >= filter.StartDate)
                .Where(row => filter.EndDate is null || row.Row.CreatedAtUtc <= filter.EndDate)
                .Take(maxRows)
                .Select(row => row.Row)
                .ToArray();

            return Task.FromResult(rows);
        }

        public Task<IReadOnlyCollection<StoreExportRow>> ListStoresAsync(
            Guid userId,
            ExportFilter filter,
            int maxRows,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<StoreExportRow> rows = [];
            return Task.FromResult(rows);
        }

        public Task<IReadOnlyCollection<PriceHistoryExportRow>> ListPriceHistoriesAsync(
            Guid userId,
            ExportFilter filter,
            int maxRows,
            CancellationToken cancellationToken = default)
        {
            LastPriceHistoryFilter = filter;
            IReadOnlyCollection<PriceHistoryExportRow> rows = PriceHistories
                .Where(row => row.UserId == userId)
                .Where(row => filter.ProductId is null || row.Row.ProductId == filter.ProductId)
                .Where(row => filter.StoreId is null || row.Row.StoreId == filter.StoreId)
                .Where(row => filter.StartDate is null || row.Row.CapturedAt >= filter.StartDate)
                .Where(row => filter.EndDate is null || row.Row.CapturedAt <= filter.EndDate)
                .Take(maxRows)
                .Select(row => row.Row)
                .ToArray();

            return Task.FromResult(rows);
        }

        public Task<IReadOnlyCollection<AlertNotificationExportRow>> ListAlertNotificationsAsync(
            Guid userId,
            ExportFilter filter,
            int maxRows,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<AlertNotificationExportRow> rows = [];
            return Task.FromResult(rows);
        }
    }

    private sealed record UserProductExportRow(Guid UserId, ProductExportRow Row);

    private sealed record UserPriceHistoryExportRow(Guid UserId, PriceHistoryExportRow Row);
}
