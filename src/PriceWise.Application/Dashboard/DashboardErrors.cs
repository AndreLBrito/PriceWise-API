using PriceWise.Application.Common;

namespace PriceWise.Application.Dashboard;

public static class DashboardErrors
{
    public static readonly Error ProductNotFound = new(
        "Dashboard.ProductNotFound",
        "Produto não encontrado.");

    public static readonly Error StoreNotFound = new(
        "Dashboard.StoreNotFound",
        "Loja não encontrada.");
}
