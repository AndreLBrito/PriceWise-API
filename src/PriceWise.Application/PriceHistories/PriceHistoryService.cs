using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.AlertNotifications;
using PriceWise.Application.Common;
using PriceWise.Application.PriceHistories.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.PriceHistories;

public sealed class PriceHistoryService : IPriceHistoryService
{
    private readonly IPriceHistoryRepository priceHistoryRepository;
    private readonly IProductRepository productRepository;
    private readonly IStoreRepository storeRepository;
    private readonly IAlertNotificationService alertNotificationService;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;
    private readonly IApplicationTelemetry telemetry;

    public PriceHistoryService(
        IPriceHistoryRepository priceHistoryRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IAlertNotificationService alertNotificationService,
        IDashboardCacheInvalidator dashboardCacheInvalidator,
        IApplicationTelemetry telemetry)
    {
        this.priceHistoryRepository = priceHistoryRepository;
        this.productRepository = productRepository;
        this.storeRepository = storeRepository;
        this.alertNotificationService = alertNotificationService;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
        this.telemetry = telemetry;
    }

    public async Task<Result<PriceHistoryResponse>> CreateAsync(
        Guid userId,
        CreatePriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceHistoryService.Create");
        var product = await productRepository.GetByIdAsync(request.ProductId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceHistoryErrors.ProductNotFound.Code);
            return Result<PriceHistoryResponse>.Failure(PriceHistoryErrors.ProductNotFound);
        }

        var store = await storeRepository.GetByIdAsync(request.StoreId, userId, cancellationToken);

        if (store is null)
        {
            telemetry.RecordError(PriceHistoryErrors.StoreNotFound.Code);
            return Result<PriceHistoryResponse>.Failure(PriceHistoryErrors.StoreNotFound);
        }

        var priceHistory = PriceHistory.Create(
            userId,
            request.ProductId,
            request.StoreId,
            request.Price,
            request.Currency,
            request.CapturedAt,
            request.SourceUrl);

        await priceHistoryRepository.AddAsync(priceHistory, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, priceHistory.ProductId, cancellationToken);
        await dashboardCacheInvalidator.InvalidateStoreSummaryAsync(userId, priceHistory.StoreId, cancellationToken);
        await alertNotificationService.CheckPriceAlertsAsync(priceHistory, cancellationToken);
        telemetry.RecordPriceHistoryCreated();

        return Result<PriceHistoryResponse>.Success(MapToResponse(priceHistory));
    }

    public async Task<Result<IReadOnlyCollection<PriceHistoryResponse>>> ListByProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceHistoryService.ListByProduct");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceHistoryErrors.ProductNotFound.Code);
            return Result<IReadOnlyCollection<PriceHistoryResponse>>.Failure(PriceHistoryErrors.ProductNotFound);
        }

        var histories = await priceHistoryRepository.ListByProductAsync(userId, productId, cancellationToken);
        var response = histories.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<PriceHistoryResponse>>.Success(response);
    }

    public async Task<Result<PriceHistoryResponse>> GetLatestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceHistoryService.GetLatest");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceHistoryErrors.ProductNotFound.Code);
            return Result<PriceHistoryResponse>.Failure(PriceHistoryErrors.ProductNotFound);
        }

        var priceHistory = await priceHistoryRepository.GetLatestAsync(userId, productId, cancellationToken);

        if (priceHistory is null)
        {
            telemetry.RecordError(PriceHistoryErrors.PriceHistoryNotFound.Code);
            return Result<PriceHistoryResponse>.Failure(PriceHistoryErrors.PriceHistoryNotFound);
        }

        return Result<PriceHistoryResponse>.Success(MapToResponse(priceHistory));
    }

    public async Task<Result<PriceHistoryResponse>> GetLowestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceHistoryService.GetLowest");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceHistoryErrors.ProductNotFound.Code);
            return Result<PriceHistoryResponse>.Failure(PriceHistoryErrors.ProductNotFound);
        }

        var priceHistory = await priceHistoryRepository.GetLowestAsync(userId, productId, cancellationToken);

        if (priceHistory is null)
        {
            telemetry.RecordError(PriceHistoryErrors.PriceHistoryNotFound.Code);
            return Result<PriceHistoryResponse>.Failure(PriceHistoryErrors.PriceHistoryNotFound);
        }

        return Result<PriceHistoryResponse>.Success(MapToResponse(priceHistory));
    }

    public async Task<Result<AveragePriceHistoryResponse>> GetAverageAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceHistoryService.GetAverage");
        var product = await productRepository.GetByIdAsync(productId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceHistoryErrors.ProductNotFound.Code);
            return Result<AveragePriceHistoryResponse>.Failure(PriceHistoryErrors.ProductNotFound);
        }

        var average = await priceHistoryRepository.GetAverageAsync(userId, productId, cancellationToken);

        if (average is null)
        {
            telemetry.RecordError(PriceHistoryErrors.PriceHistoryNotFound.Code);
            return Result<AveragePriceHistoryResponse>.Failure(PriceHistoryErrors.PriceHistoryNotFound);
        }

        return Result<AveragePriceHistoryResponse>.Success(
            new AveragePriceHistoryResponse(productId, average.Value.AveragePrice, average.Value.EntriesCount));
    }

    private static PriceHistoryResponse MapToResponse(PriceHistory priceHistory)
    {
        return new PriceHistoryResponse(
            priceHistory.Id,
            priceHistory.ProductId,
            priceHistory.StoreId,
            priceHistory.Price,
            priceHistory.Currency,
            priceHistory.CapturedAt,
            priceHistory.SourceUrl,
            priceHistory.CreatedAtUtc,
            priceHistory.UpdatedAtUtc);
    }
}
