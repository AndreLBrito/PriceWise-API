using PriceWise.Application.Abstractions.Caching;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.Stores.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Stores;

public sealed class StoreService : IStoreService
{
    private readonly IStoreRepository storeRepository;
    private readonly IDashboardCacheInvalidator dashboardCacheInvalidator;

    public StoreService(
        IStoreRepository storeRepository,
        IDashboardCacheInvalidator dashboardCacheInvalidator)
    {
        this.storeRepository = storeRepository;
        this.dashboardCacheInvalidator = dashboardCacheInvalidator;
    }

    public async Task<Result<StoreResponse>> CreateAsync(
        Guid userId,
        CreateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = NormalizeUrl(request.BaseUrl);
        var existingStore = await storeRepository.GetByBaseUrlAsync(userId, baseUrl, cancellationToken);

        if (existingStore is not null)
        {
            return Result<StoreResponse>.Failure(StoreErrors.BaseUrlAlreadyRegistered);
        }

        var store = Store.Create(userId, request.Name, baseUrl, request.LogoUrl);

        await storeRepository.AddAsync(store, cancellationToken);
        await dashboardCacheInvalidator.InvalidateStoreSummaryAsync(userId, store.Id, cancellationToken);

        return Result<StoreResponse>.Success(MapToResponse(store));
    }

    public async Task<Result<IReadOnlyCollection<StoreResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var stores = await storeRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = stores.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<StoreResponse>>.Success(response);
    }

    public async Task<Result<StoreResponse>> GetByIdAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        return store is null
            ? Result<StoreResponse>.Failure(StoreErrors.StoreNotFound)
            : Result<StoreResponse>.Success(MapToResponse(store));
    }

    public async Task<Result<StoreResponse>> UpdateAsync(
        Guid userId,
        Guid storeId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        if (store is null)
        {
            return Result<StoreResponse>.Failure(StoreErrors.StoreNotFound);
        }

        var baseUrl = NormalizeUrl(request.BaseUrl);
        var existingStore = await storeRepository.GetByBaseUrlAsync(userId, baseUrl, cancellationToken);

        if (existingStore is not null && existingStore.Id != store.Id)
        {
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
        var store = await storeRepository.GetByIdAsync(storeId, userId, cancellationToken);

        if (store is null)
        {
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
