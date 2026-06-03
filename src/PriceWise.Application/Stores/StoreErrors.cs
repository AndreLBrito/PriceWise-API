using PriceWise.Application.Common;

namespace PriceWise.Application.Stores;

public static class StoreErrors
{
    public static readonly Error BaseUrlAlreadyRegistered = new(
        "Stores.BaseUrlAlreadyRegistered",
        "Já existe uma loja cadastrada com esta URL base.");

    public static readonly Error StoreNotFound = new(
        "Stores.StoreNotFound",
        "Loja não encontrada.");
}
