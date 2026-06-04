namespace PriceWise.Application.NotificationChannels.Dtos;

public sealed record UpdateNotificationChannelRequest(
    string Type,
    string Name,
    string Destination);
