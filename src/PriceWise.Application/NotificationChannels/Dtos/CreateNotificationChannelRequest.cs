namespace PriceWise.Application.NotificationChannels.Dtos;

public sealed record CreateNotificationChannelRequest(
    string Type,
    string Name,
    string Destination);
