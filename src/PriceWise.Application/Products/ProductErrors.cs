using PriceWise.Application.Common;

namespace PriceWise.Application.Products;

public static class ProductErrors
{
    public static readonly Error ProductUrlAlreadyRegistered = new(
        "Products.ProductUrlAlreadyRegistered",
        "Já existe um produto cadastrado com esta URL.");

    public static readonly Error ProductNotFound = new(
        "Products.ProductNotFound",
        "Produto não encontrado.");
}
