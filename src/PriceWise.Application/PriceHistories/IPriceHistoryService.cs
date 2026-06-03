using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.PriceHistories.Dtos;

namespace PriceWise.Application.PriceHistories;

public interface IPriceHistoryService : IService
{
    Task<Result<PriceHistoryResponse>> CreateAsync(
        Guid userId,
        CreatePriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<PriceHistoryResponse>>> ListByProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Result<PriceHistoryResponse>> GetLatestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Result<PriceHistoryResponse>> GetLowestAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Result<AveragePriceHistoryResponse>> GetAverageAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);
}
