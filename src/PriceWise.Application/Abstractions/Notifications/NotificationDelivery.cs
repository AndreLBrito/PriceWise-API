using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Notifications;

public sealed record NotificationDelivery(
    AlertNotification AlertNotification,
    NotificationChannel Channel);
