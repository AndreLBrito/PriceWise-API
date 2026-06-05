using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.AlertNotifications.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.AlertNotifications;

public interface IAlertNotificationService : IService
{
    Task CheckPriceAlertsAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<AlertNotificationResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<Result<PagedResponse<AlertNotificationResponse>>> ListAsync(
        Guid userId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await ListAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Result<PagedResponse<AlertNotificationResponse>>.Success(PagedResponse<AlertNotificationResponse>.Create(
                result.Value.Skip(request.Offset).Take(request.NormalizedPageSize).ToArray(),
                request.NormalizedPage,
                request.NormalizedPageSize,
                result.Value.Count))
            : Result<PagedResponse<AlertNotificationResponse>>.Failure(result.Error);
    }

    Task<Result<AlertNotificationResponse>> GetByIdAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
