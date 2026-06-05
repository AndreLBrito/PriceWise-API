using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
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
    private readonly IPriceProvider priceProvider;
    private readonly PriceCheckOptions options;
    private readonly ILogger<PriceCheckService> logger;
    private readonly IApplicationTelemetry telemetry;

    public PriceCheckService(
        IPriceCheckRepository priceCheckRepository,
        IPriceHistoryService priceHistoryService,
        IPriceProvider priceProvider,
        IOptions<PriceCheckOptions> options,
        ILogger<PriceCheckService> logger,
        IApplicationTelemetry telemetry)
    {
        this.priceCheckRepository = priceCheckRepository;
        this.priceHistoryService = priceHistoryService;
        this.priceProvider = priceProvider;
        this.options = options.Value;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task<Result<PriceCheckRunResponse>> RunAsync(CancellationToken cancellationToken = default)
    {
        return await RunAsync(PriceCheckTrigger.Manual, cancellationToken);
    }

    public async Task<Result<PriceCheckRunResponse>> RunAsync(
        PriceCheckTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceCheckService.Run");
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

                var simulatedPrice = await priceProvider.GetCurrentPriceAsync(candidate, cancellationToken);
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
            telemetry.RecordPriceCheck(trigger);

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
            telemetry.RecordError(exception);
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
        using var activity = telemetry.StartActivity("PriceCheckService.GetStatus");
        var lastExecution = await priceCheckRepository.GetLastExecutionAsync(cancellationToken);

        return Result<PriceCheckStatusResponse>.Success(new PriceCheckStatusResponse(
            options.Enabled,
            Math.Max(1, options.IntervalInMinutes),
            Math.Max(1, options.MaxProductsPerExecution),
            lastExecution?.CompletedAt,
            lastExecution?.Status,
            lastExecution?.Message));
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
