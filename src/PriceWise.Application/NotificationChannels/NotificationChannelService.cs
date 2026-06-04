using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.NotificationChannels.Dtos;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.NotificationChannels;

public sealed class NotificationChannelService : INotificationChannelService
{
    private readonly INotificationChannelRepository notificationChannelRepository;

    public NotificationChannelService(INotificationChannelRepository notificationChannelRepository)
    {
        this.notificationChannelRepository = notificationChannelRepository;
    }

    public async Task<Result<NotificationChannelResponse>> CreateAsync(
        Guid userId,
        CreateNotificationChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseType(request.Type, out var type))
        {
            return Result<NotificationChannelResponse>.Failure(NotificationChannelErrors.InvalidType);
        }

        var destination = NormalizeDestination(type, request.Destination);
        var existingChannel = await notificationChannelRepository.GetActiveByTypeAndDestinationAsync(
            userId,
            type,
            destination,
            cancellationToken);

        if (existingChannel is not null)
        {
            return Result<NotificationChannelResponse>.Failure(NotificationChannelErrors.DuplicateChannel);
        }

        var notificationChannel = NotificationChannel.Create(
            userId,
            type,
            request.Name,
            destination);

        await notificationChannelRepository.AddAsync(notificationChannel, cancellationToken);

        return Result<NotificationChannelResponse>.Success(MapToResponse(notificationChannel));
    }

    public async Task<Result<IReadOnlyCollection<NotificationChannelResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var channels = await notificationChannelRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = channels.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<NotificationChannelResponse>>.Success(response);
    }

    public async Task<Result<NotificationChannelResponse>> GetByIdAsync(
        Guid userId,
        Guid notificationChannelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await notificationChannelRepository.GetByIdAsync(
            notificationChannelId,
            userId,
            cancellationToken);

        return channel is null
            ? Result<NotificationChannelResponse>.Failure(NotificationChannelErrors.NotificationChannelNotFound)
            : Result<NotificationChannelResponse>.Success(MapToResponse(channel));
    }

    public async Task<Result<NotificationChannelResponse>> UpdateAsync(
        Guid userId,
        Guid notificationChannelId,
        UpdateNotificationChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        var channel = await notificationChannelRepository.GetByIdAsync(
            notificationChannelId,
            userId,
            cancellationToken);

        if (channel is null)
        {
            return Result<NotificationChannelResponse>.Failure(NotificationChannelErrors.NotificationChannelNotFound);
        }

        if (!TryParseType(request.Type, out var type))
        {
            return Result<NotificationChannelResponse>.Failure(NotificationChannelErrors.InvalidType);
        }

        var destination = NormalizeDestination(type, request.Destination);
        var existingChannel = await notificationChannelRepository.GetActiveByTypeAndDestinationAsync(
            userId,
            type,
            destination,
            cancellationToken);

        if (existingChannel is not null && existingChannel.Id != channel.Id)
        {
            return Result<NotificationChannelResponse>.Failure(NotificationChannelErrors.DuplicateChannel);
        }

        channel.Update(type, request.Name, destination);
        await notificationChannelRepository.UpdateAsync(channel, cancellationToken);

        return Result<NotificationChannelResponse>.Success(MapToResponse(channel));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        Guid notificationChannelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await notificationChannelRepository.GetByIdAsync(
            notificationChannelId,
            userId,
            cancellationToken);

        if (channel is null)
        {
            return Result.Failure(NotificationChannelErrors.NotificationChannelNotFound);
        }

        channel.Deactivate();
        await notificationChannelRepository.UpdateAsync(channel, cancellationToken);

        return Result.Success();
    }

    private static NotificationChannelResponse MapToResponse(NotificationChannel channel)
    {
        return new NotificationChannelResponse(
            channel.Id,
            channel.Type.ToString(),
            channel.Name,
            channel.Destination,
            channel.IsActive,
            channel.CreatedAtUtc,
            channel.UpdatedAtUtc);
    }

    private static bool TryParseType(string type, out NotificationChannelType channelType)
    {
        return Enum.TryParse(type, true, out channelType);
    }

    private static string NormalizeDestination(NotificationChannelType type, string destination)
    {
        var trimmedDestination = destination.Trim();

        return type == NotificationChannelType.Email
            ? trimmedDestination.ToLowerInvariant()
            : trimmedDestination;
    }
}
