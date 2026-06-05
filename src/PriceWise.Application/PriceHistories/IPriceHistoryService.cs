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

    async Task<Result<PagedResponse<PriceHistoryResponse>>> ListByProductAsync(
        Guid userId,
        Guid productId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await ListByProductAsync(userId, productId, cancellationToken);

        return result.IsSuccess
            ? Result<PagedResponse<PriceHistoryResponse>>.Success(PagedResponse<PriceHistoryResponse>.Create(
                result.Value.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
                request.NormalizedPage,
                request.NormalizedPageSize,
                result.Value.Count))
            : Result<PagedResponse<PriceHistoryResponse>>.Failure(result.Error);
    }

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
