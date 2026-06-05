using PriceWise.Application.Common;

namespace PriceWise.Application.Outbox;

public static class OutboxErrors
{
    public static readonly Error MessageNotFound = new(
        "Outbox.MessageNotFound",
        "Mensagem da outbox não encontrada.");

    public static readonly Error RetryOnlyFailedMessages = new(
        "Outbox.RetryOnlyFailedMessages",
        "Somente mensagens com falha podem ser reenfileiradas.");
}
