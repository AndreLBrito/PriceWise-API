using PriceWise.Application.Common;

namespace PriceWise.Application.PriceHistories;

public static class PriceHistoryErrors
{
    public static readonly Error ProductNotFound = new(
        "PriceHistories.ProductNotFound",
        "Produto não encontrado ou inativo.");

    public static readonly Error StoreNotFound = new(
        "PriceHistories.StoreNotFound",
        "Loja não encontrada ou inativa.");

    public static readonly Error PriceHistoryNotFound = new(
        "PriceHistories.PriceHistoryNotFound",
        "Histórico de preço não encontrado.");
}
