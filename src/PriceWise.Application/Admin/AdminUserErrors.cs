using PriceWise.Application.Common;

namespace PriceWise.Application.Admin;

public static class AdminUserErrors
{
    public static readonly Error UserNotFound = new(
        "Admin.UserNotFound",
        "Usuário não encontrado.");

    public static readonly Error InvalidRole = new(
        "Admin.InvalidRole",
        "Papel informado inválido.");

    public static readonly Error CannotDeactivateSelf = new(
        "Admin.CannotDeactivateSelf",
        "Você não pode desativar a própria conta.");

    public static readonly Error CannotRemoveOwnAdminRole = new(
        "Admin.CannotRemoveOwnAdminRole",
        "Você não pode remover o próprio papel de administrador.");
}
