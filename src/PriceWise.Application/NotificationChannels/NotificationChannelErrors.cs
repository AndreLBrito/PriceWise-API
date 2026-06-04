using PriceWise.Application.Common;

namespace PriceWise.Application.NotificationChannels;

public static class NotificationChannelErrors
{
    public static readonly Error InvalidType = new(
        "NotificationChannels.InvalidType",
        "Tipo de canal de notificação inválido.");

    public static readonly Error DuplicateChannel = new(
        "NotificationChannels.DuplicateChannel",
        "Já existe um canal ativo com este tipo e destino.");

    public static readonly Error NotificationChannelNotFound = new(
        "NotificationChannels.NotificationChannelNotFound",
        "Canal de notificação não encontrado.");
}
