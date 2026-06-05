using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Common;
using PriceWise.Application.PriceChecks;
using PriceWise.Application.PriceHistories;
using PriceWise.Application.PriceHistories.Dtos;

namespace PriceWise.Tests.Unit.PriceChecks;

public sealed class PriceCheckServiceTests
{
    [Fact]
    public async Task RunAsyncCreatesBasePriceWhenProductHasNoHistory()
    {
        var repository = new InMemoryPriceCheckRepository();
        var priceHistoryService = new FakePriceHistoryService();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        repository.Candidates.Add(new PriceCheckCandidate(
            userId,
            productId,
            storeId,
            "https://example.com/product/1",
            null,
            null));
        var priceProvider = new FakePriceProvider(123.45m);
        var service = CreateService(repository, priceHistoryService, priceProvider);

        var result = await service.RunAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.HistoriesCreated.Should().Be(1);
        priceHistoryService.CreatedRequests.Should().ContainSingle();
        priceHistoryService.CreatedRequests[0].Request.Price.Should().Be(123.45m);
        priceHistoryService.CreatedRequests[0].Request.Currency.Should().Be("BRL");
        repository.Executions.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsyncVariesPriceLightlyFromLatestPrice()
    {
        var repository = new InMemoryPriceCheckRepository();
        var priceHistoryService = new FakePriceHistoryService();
        repository.Candidates.Add(new PriceCheckCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.com/product/1",
            100m,
            DateTime.UtcNow.AddHours(-2)));
        var service = CreateService(repository, priceHistoryService, new FakePriceProvider(101.50m));

        var result = await service.RunAsync();

        result.IsSuccess.Should().BeTrue();
        priceHistoryService.CreatedRequests.Should().ContainSingle();
        priceHistoryService.CreatedRequests[0].Request.Price.Should().Be(101.50m);
    }

    [Fact]
    public async Task RunAsyncSkipsProductWithRecentHistory()
    {
        var repository = new InMemoryPriceCheckRepository();
        var priceHistoryService = new FakePriceHistoryService();
        repository.Candidates.Add(new PriceCheckCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.com/product/1",
            100m,
            DateTime.UtcNow.AddMinutes(-5)));
        var service = CreateService(repository, priceHistoryService);

        var result = await service.RunAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.ProductsSkipped.Should().Be(1);
        priceHistoryService.CreatedRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsyncUsesPriceProvider()
    {
        var repository = new InMemoryPriceCheckRepository();
        var priceHistoryService = new FakePriceHistoryService();
        var provider = new FakePriceProvider(88.90m);
        var candidate = new PriceCheckCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.com/product/1",
            100m,
            DateTime.UtcNow.AddHours(-2));
        repository.Candidates.Add(candidate);
        var service = CreateService(repository, priceHistoryService, provider);

        var result = await service.RunAsync();

        result.IsSuccess.Should().BeTrue();
        provider.Candidates.Should().ContainSingle().Which.Should().Be(candidate);
        priceHistoryService.CreatedRequests.Should().ContainSingle();
        priceHistoryService.CreatedRequests[0].Request.Price.Should().Be(88.90m);
    }

    [Fact]
    public async Task GetStatusAsyncReturnsOptionsAndLastExecution()
    {
        var repository = new InMemoryPriceCheckRepository();
        var lastExecution = new PriceCheckExecution(
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            "Concluída",
            "Verificação concluída.",
            1,
            1,
            0,
            0);
        repository.Executions.Add(lastExecution);
        var service = CreateService(repository, new FakePriceHistoryService());

        var result = await service.GetStatusAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Enabled.Should().BeTrue();
        result.Value.IntervalInMinutes.Should().Be(30);
        result.Value.MaxProductsPerExecution.Should().Be(50);
        result.Value.LastExecutionAt.Should().Be(lastExecution.CompletedAt);
        result.Value.LastExecutionStatus.Should().Be("Concluída");
    }

    private static PriceCheckService CreateService(
        InMemoryPriceCheckRepository repository,
        FakePriceHistoryService priceHistoryService,
        IPriceProvider? priceProvider = null)
    {
        var options = Options.Create(new PriceCheckOptions
        {
            Enabled = true,
            IntervalInMinutes = 30,
            MaxProductsPerExecution = 50
        });

        return new PriceCheckService(
            repository,
            priceHistoryService,
            priceProvider ?? new FakePriceProvider(100m),
            options,
            NullLogger<PriceCheckService>.Instance,
            new NoOpApplicationTelemetry());
    }

    private sealed class InMemoryPriceCheckRepository : IPriceCheckRepository
    {
        public List<PriceCheckCandidate> Candidates { get; } = [];

        public List<PriceCheckExecution> Executions { get; } = [];

        public Task<IReadOnlyCollection<PriceCheckCandidate>> ListCandidatesAsync(
            int maxProducts,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PriceCheckCandidate> candidates = Candidates
                .Take(maxProducts)
                .ToArray();

            return Task.FromResult(candidates);
        }

        public Task AddExecutionAsync(
            PriceCheckExecution execution,
            CancellationToken cancellationToken = default)
        {
            Executions.Add(execution);

            return Task.CompletedTask;
        }

        public Task<PriceCheckExecution?> GetLastExecutionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Executions.LastOrDefault());
        }
    }

    private sealed class FakePriceHistoryService : IPriceHistoryService
    {
        public List<(Guid UserId, CreatePriceHistoryRequest Request)> CreatedRequests { get; } = [];

        public Task<Result<PriceHistoryResponse>> CreateAsync(
            Guid userId,
            CreatePriceHistoryRequest request,
            CancellationToken cancellationToken = default)
        {
            CreatedRequests.Add((userId, request));

            return Task.FromResult(Result<PriceHistoryResponse>.Success(new PriceHistoryResponse(
                Guid.NewGuid(),
                request.ProductId,
                request.StoreId,
                request.Price,
                request.Currency,
                request.CapturedAt ?? DateTime.UtcNow,
                request.SourceUrl,
                DateTime.UtcNow,
                null)));
        }

        public Task<Result<IReadOnlyCollection<PriceHistoryResponse>>> ListByProductAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<PriceHistoryResponse>> GetLatestAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<PriceHistoryResponse>> GetLowestAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<AveragePriceHistoryResponse>> GetAverageAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakePriceProvider : IPriceProvider
    {
        private readonly decimal price;

        public FakePriceProvider(decimal price)
        {
            this.price = price;
        }

        public List<PriceCheckCandidate> Candidates { get; } = [];

        public Task<decimal> GetCurrentPriceAsync(
            PriceCheckCandidate candidate,
            CancellationToken cancellationToken = default)
        {
            Candidates.Add(candidate);

            return Task.FromResult(price);
        }
    }
}
