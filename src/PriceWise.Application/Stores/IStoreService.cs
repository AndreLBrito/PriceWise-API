using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.Stores.Dtos;

namespace PriceWise.Application.Stores;

public interface IStoreService : IService
{
    Task<Result<StoreResponse>> CreateAsync(
        Guid userId,
        CreateStoreRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<StoreResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<StoreResponse>> GetByIdAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<Result<StoreResponse>> UpdateAsync(
        Guid userId,
        Guid storeId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default);
}
