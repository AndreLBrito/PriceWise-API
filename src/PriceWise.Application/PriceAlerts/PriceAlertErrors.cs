using PriceWise.Application.Common;

namespace PriceWise.Application.PriceAlerts;

public static class PriceAlertErrors
{
    public static readonly Error ProductNotFound = new(
        "PriceAlerts.ProductNotFound",
        "Produto não encontrado ou inativo.");

    public static readonly Error ActiveAlertAlreadyExists = new(
        "PriceAlerts.ActiveAlertAlreadyExists",
        "Já existe um alerta ativo para este produto.");

    public static readonly Error PriceAlertNotFound = new(
        "PriceAlerts.PriceAlertNotFound",
        "Alerta de preço não encontrado.");
}
