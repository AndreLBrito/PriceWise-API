using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Common;
using PriceWise.Application.Stores.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Stores;

public sealed class StoreService : IStoreService
{
    private readonly IStoreRepository storeRepository;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;
    private readonly IApplicationTelemetry telemetry;

    public StoreService(
        IStoreRepository storeRepository,
        IDashboardCacheInvalidator dashboardCacheInvalidator,
        IApplicationTelemetry telemetry)
    {
        this.storeRepository = storeRepository;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
        this.telemetry = telemetry;
    }

    public async Task<Result<StoreResponse>> CreateAsync(
        Guid userId,
        CreateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("StoreService.Create");
        var baseUrl = NormalizeUrl(request.BaseUrl);
        var existingStore = await storeRepository.GetByBaseUrlAsync(userId, baseUrl, cancellationToken);

        if (existingStore is not null)
        {
            telemetry.RecordError(StoreErrors.BaseUrlAlreadyRegistered.Code);
            return Result<StoreResponse>.Failure(StoreErrors.BaseUrlAlreadyRegistered);
        }

        var store = Store.Create(userId, request.Name, baseUrl, request.LogoUrl);

        await storeRepository.AddAsync(store, cancellationToken);
        await dashboardCacheInvalidator.InvalidateStoreSummaryAsync(userId, store.Id, cancellationToken);
        telemetry.RecordStoreCreated();

        return Result<StoreResponse>.Success(MapToResponse(store));
    }

    public async Task<Result<IReadOnlyCollection<StoreResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("StoreService.List");
        var stores = await storeRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = stores.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<StoreResponse>>.Success(response);
    }

    public async Task<Result<StoreResponse>> GetByIdAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("StoreService.GetById");
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        if (store is null)
        {
            telemetry.RecordError(StoreErrors.StoreNotFound.Code);
            return Result<StoreResponse>.Failure(StoreErrors.StoreNotFound);
        }

        return Result<StoreResponse>.Success(MapToResponse(store));
    }

    public async Task<Result<StoreResponse>> UpdateAsync(
        Guid userId,
        Guid storeId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("StoreService.Update");
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        if (store is null)
        {
            telemetry.RecordError(StoreErrors.StoreNotFound.Code);
            return Result<StoreResponse>.Failure(StoreErrors.StoreNotFound);
        }

        var baseUrl = NormalizeUrl(request.BaseUrl);
        var existingStore = await storeRepository.GetByBaseUrlAsync(userId, baseUrl, cancellationToken);

        if (existingStore is not null && existingStore.Id != store.Id)
        {
            telemetry.RecordError(StoreErrors.BaseUrlAlreadyRegistered.Code);
            return Result<StoreResponse>.Failure(StoreErrors.BaseUrlAlreadyRegistered);
        }

        store.Update(request.Name, baseUrl, request.LogoUrl);

        await storeRepository.UpdateAsync(store, cancellationToken);
        await dashboardCacheInvalidator.InvalidateStoreSummaryAsync(userId, store.Id, cancellationToken);

        return Result<StoreResponse>.Success(MapToResponse(store));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("StoreService.Delete");
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        if (store is null)
        {
            telemetry.RecordError(StoreErrors.StoreNotFound.Code);
            return Result.Failure(StoreErrors.StoreNotFound);
        }

        store.Deactivate();
        await storeRepository.UpdateAsync(store, cancellationToken);
        await dashboardCacheInvalidator.InvalidateStoreSummaryAsync(userId, store.Id, cancellationToken);

        return Result.Success();
    }

    private static StoreResponse MapToResponse(Store store)
    {
        return new StoreResponse(
            store.Id,
            store.Name,
            store.BaseUrl,
            store.LogoUrl,
            store.IsActive,
            store.CreatedAtUtc,
            store.UpdatedAtUtc);
    }

    private static string NormalizeUrl(string baseUrl)
    {
        return baseUrl.Trim();
    }
}
