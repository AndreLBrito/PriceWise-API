namespace PriceWise.Application.NotificationChannels.Dtos;

public sealed record NotificationChannelResponse(
    Guid Id,
    string Type,
    string Name,
    string Destination,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
