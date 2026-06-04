using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.NotificationChannels.Dtos;

namespace PriceWise.Application.NotificationChannels;

public interface INotificationChannelService : IService
{
    Task<Result<NotificationChannelResponse>> CreateAsync(
        Guid userId,
        CreateNotificationChannelRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<NotificationChannelResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationChannelResponse>> GetByIdAsync(
        Guid userId,
        Guid notificationChannelId,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationChannelResponse>> UpdateAsync(
        Guid userId,
        Guid notificationChannelId,
        UpdateNotificationChannelRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid userId,
        Guid notificationChannelId,
        CancellationToken cancellationToken = default);
}
