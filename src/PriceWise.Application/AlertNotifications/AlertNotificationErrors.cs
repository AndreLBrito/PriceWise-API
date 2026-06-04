using PriceWise.Application.Common;

namespace PriceWise.Application.AlertNotifications;

public static class AlertNotificationErrors
{
    public static readonly Error AlertNotificationNotFound = new(
        "AlertNotifications.AlertNotificationNotFound",
        "Notificação de alerta não encontrada.");
}
