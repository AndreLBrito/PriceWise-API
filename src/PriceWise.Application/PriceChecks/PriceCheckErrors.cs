using PriceWise.Application.Common;

namespace PriceWise.Application.PriceChecks;

public static class PriceCheckErrors
{
    public static readonly Error ExecutionFailed = new(
        "PriceCheck.ExecutionFailed",
        "Não foi possível executar a verificação de preços.");
}
