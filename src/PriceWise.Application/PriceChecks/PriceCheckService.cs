using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.PriceChecks.Dtos;
using PriceWise.Application.PriceHistories;
using PriceWise.Application.PriceHistories.Dtos;

namespace PriceWise.Application.PriceChecks;

public sealed class PriceCheckService : IPriceCheckService
{
    private const string Currency = "BRL";
    private const string CompletedStatus = "Concluída";
    private const string CompletedWithErrorsStatus = "Concluída com falhas";
    private const string FailedStatus = "Falhou";

    private readonly IPriceCheckRepository priceCheckRepository;
    private readonly IPriceHistoryService priceHistoryService;
    private readonly PriceCheckOptions options;
    private readonly ILogger<PriceCheckService> logger;

    public PriceCheckService(
        IPriceCheckRepository priceCheckRepository,
        IPriceHistoryService priceHistoryService,
        IOptions<PriceCheckOptions> options,
        ILogger<PriceCheckService> logger)
    {
        this.priceCheckRepository = priceCheckRepository;
        this.priceHistoryService = priceHistoryService;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<Result<PriceCheckRunResponse>> RunAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var maxProducts = Math.Max(1, options.MaxProductsPerExecution);
        var intervalInMinutes = Math.Max(1, options.IntervalInMinutes);

        try
        {
            var candidates = await priceCheckRepository.ListCandidatesAsync(maxProducts, cancellationToken);
            var recentHistoryCutoff = startedAt.AddMinutes(-intervalInMinutes);
            var historiesCreated = 0;
            var productsSkipped = 0;
            var productsFailed = 0;

            foreach (var candidate in candidates)
            {
                if (candidate.LatestCapturedAt is not null && candidate.LatestCapturedAt >= recentHistoryCutoff)
                {
                    productsSkipped++;
                    continue;
                }

                var simulatedPrice = GeneratePrice(candidate.LatestPrice);
                var request = new CreatePriceHistoryRequest(
                    candidate.ProductId,
                    candidate.StoreId,
                    simulatedPrice,
                    Currency,
                    startedAt,
                    candidate.ProductUrl);

                var result = await priceHistoryService.CreateAsync(candidate.UserId, request, cancellationToken);

                if (result.IsSuccess)
                {
                    historiesCreated++;
                    continue;
                }

                productsFailed++;
                logger.LogWarning(
                    "Price check failed for product {ProductId} and store {StoreId}. ErrorCode: {ErrorCode}",
                    candidate.ProductId,
                    candidate.StoreId,
                    result.Error.Code);
            }

            var status = productsFailed > 0 ? CompletedWithErrorsStatus : CompletedStatus;
            var message = BuildMessage(candidates.Count, historiesCreated, productsSkipped, productsFailed);
            var completedAt = DateTime.UtcNow;

            await priceCheckRepository.AddExecutionAsync(
                new PriceCheckExecution(
                    Guid.NewGuid(),
                    startedAt,
                    completedAt,
                    status,
                    message,
                    candidates.Count,
                    historiesCreated,
                    productsSkipped,
                    productsFailed),
                cancellationToken);

            logger.LogInformation(
                "Price check completed with status {Status}. Checked: {ProductsChecked}, Created: {HistoriesCreated}, Skipped: {ProductsSkipped}, Failed: {ProductsFailed}",
                status,
                candidates.Count,
                historiesCreated,
                productsSkipped,
                productsFailed);

            return Result<PriceCheckRunResponse>.Success(new PriceCheckRunResponse(
                completedAt,
                status,
                message,
                candidates.Count,
                historiesCreated,
                productsSkipped,
                productsFailed));
        }
        catch (Exception exception)
        {
            var completedAt = DateTime.UtcNow;
            const string message = "A verificação de preços falhou durante a execução.";

            logger.LogError(exception, "Price check execution failed");

            await priceCheckRepository.AddExecutionAsync(
                new PriceCheckExecution(
                    Guid.NewGuid(),
                    startedAt,
                    completedAt,
                    FailedStatus,
                    message,
                    0,
                    0,
                    0,
                    0),
                cancellationToken);

            return Result<PriceCheckRunResponse>.Failure(PriceCheckErrors.ExecutionFailed);
        }
    }

    public async Task<Result<PriceCheckStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var lastExecution = await priceCheckRepository.GetLastExecutionAsync(cancellationToken);

        return Result<PriceCheckStatusResponse>.Success(new PriceCheckStatusResponse(
            options.Enabled,
            Math.Max(1, options.IntervalInMinutes),
            Math.Max(1, options.MaxProductsPerExecution),
            lastExecution?.CompletedAt,
            lastExecution?.Status,
            lastExecution?.Message));
    }

    private static decimal GeneratePrice(decimal? latestPrice)
    {
        var basePrice = latestPrice is > 0 ? latestPrice.Value : Random.Shared.Next(50, 1500);
        var variationPercentage = ((decimal)Random.Shared.NextDouble() * 0.06m) - 0.03m;
        var price = decimal.Round(basePrice * (1 + variationPercentage), 2, MidpointRounding.AwayFromZero);

        return Math.Max(price, 0.01m);
    }

    private static string BuildMessage(
        int productsChecked,
        int historiesCreated,
        int productsSkipped,
        int productsFailed)
    {
        if (productsChecked == 0)
        {
            return "Nenhum produto ativo com loja ativa disponível para verificação.";
        }

        return productsFailed == 0
            ? $"Verificação concluída. Históricos criados: {historiesCreated}. Produtos ignorados: {productsSkipped}."
            : $"Verificação concluída com falhas. Históricos criados: {historiesCreated}. Produtos ignorados: {productsSkipped}. Produtos com falha: {productsFailed}.";
    }
}
