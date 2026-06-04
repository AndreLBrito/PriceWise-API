using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Common;
using PriceWise.Application.PriceAlerts.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.PriceAlerts;

public sealed class PriceAlertService : IPriceAlertService
{
    private readonly IPriceAlertRepository priceAlertRepository;
    private readonly IProductRepository productRepository;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;
    private readonly IApplicationTelemetry telemetry;

    public PriceAlertService(
        IPriceAlertRepository priceAlertRepository,
        IProductRepository productRepository,
        IDashboardCacheInvalidator dashboardCacheInvalidator,
        IApplicationTelemetry telemetry)
    {
        this.priceAlertRepository = priceAlertRepository;
        this.productRepository = productRepository;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
        this.telemetry = telemetry;
    }

    public async Task<Result<PriceAlertResponse>> CreateAsync(
        Guid userId,
        CreatePriceAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceAlertService.Create");
        var product = await productRepository.GetByIdAsync(request.ProductId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceAlertErrors.ProductNotFound.Code);
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.ProductNotFound);
        }

        var existingAlert = await priceAlertRepository.GetActiveByProductIdAsync(
            userId,
            request.ProductId,
            cancellationToken);

        if (existingAlert is not null)
        {
            telemetry.RecordError(PriceAlertErrors.ActiveAlertAlreadyExists.Code);
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.ActiveAlertAlreadyExists);
        }

        var priceAlert = PriceAlert.Create(userId, request.ProductId, request.TargetPrice);

        await priceAlertRepository.AddAsync(priceAlert, cancellationToken);
        await dashboardCacheInvalidator.InvalidateAlertSummaryAsync(userId, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, priceAlert.ProductId, cancellationToken);
        telemetry.RecordPriceAlertCreated();

        return Result<PriceAlertResponse>.Success(MapToResponse(priceAlert));
    }

    public async Task<Result<IReadOnlyCollection<PriceAlertResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceAlertService.List");
        var priceAlerts = await priceAlertRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = priceAlerts.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<PriceAlertResponse>>.Success(response);
    }

    public async Task<Result<PriceAlertResponse>> GetByIdAsync(
        Guid userId,
        Guid priceAlertId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceAlertService.GetById");
        var priceAlert = await priceAlertRepository.GetByIdAsync(priceAlertId, userId, cancellationToken);

        if (priceAlert is null)
        {
            telemetry.RecordError(PriceAlertErrors.PriceAlertNotFound.Code);
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.PriceAlertNotFound);
        }

        return Result<PriceAlertResponse>.Success(MapToResponse(priceAlert));
    }

    public async Task<Result<PriceAlertResponse>> UpdateAsync(
        Guid userId,
        Guid priceAlertId,
        UpdatePriceAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceAlertService.Update");
        var priceAlert = await priceAlertRepository.GetByIdAsync(priceAlertId, userId, cancellationToken);

        if (priceAlert is null)
        {
            telemetry.RecordError(PriceAlertErrors.PriceAlertNotFound.Code);
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.PriceAlertNotFound);
        }

        var product = await productRepository.GetByIdAsync(priceAlert.ProductId, userId, cancellationToken);

        if (product is null)
        {
            telemetry.RecordError(PriceAlertErrors.ProductNotFound.Code);
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.ProductNotFound);
        }

        priceAlert.Update(request.TargetPrice);

        await priceAlertRepository.UpdateAsync(priceAlert, cancellationToken);
        await dashboardCacheInvalidator.InvalidateAlertSummaryAsync(userId, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, priceAlert.ProductId, cancellationToken);

        return Result<PriceAlertResponse>.Success(MapToResponse(priceAlert));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        Guid priceAlertId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("PriceAlertService.Delete");
        var priceAlert = await priceAlertRepository.GetByIdAsync(priceAlertId, userId, cancellationToken);

        if (priceAlert is null)
        {
            telemetry.RecordError(PriceAlertErrors.PriceAlertNotFound.Code);
            return Result.Failure(PriceAlertErrors.PriceAlertNotFound);
        }

        priceAlert.Deactivate();
        await priceAlertRepository.UpdateAsync(priceAlert, cancellationToken);
        await dashboardCacheInvalidator.InvalidateAlertSummaryAsync(userId, cancellationToken);
        await dashboardCacheInvalidator.InvalidateProductSummaryAsync(userId, priceAlert.ProductId, cancellationToken);

        return Result.Success();
    }

    private static PriceAlertResponse MapToResponse(PriceAlert priceAlert)
    {
        return new PriceAlertResponse(
            priceAlert.Id,
            priceAlert.ProductId,
            priceAlert.TargetPrice,
            priceAlert.IsActive,
            priceAlert.CreatedAtUtc,
            priceAlert.UpdatedAtUtc);
    }
}
