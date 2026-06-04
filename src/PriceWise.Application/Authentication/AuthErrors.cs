using PriceWise.Application.Common;

namespace PriceWise.Application.Authentication;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyRegistered = new(
        "Auth.EmailAlreadyRegistered",
        "E-mail já cadastrado.");

    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "E-mail ou senha inválidos.");

    public static readonly Error InvalidRefreshToken = new(
        "Auth.InvalidRefreshToken",
        "Refresh token inválido.");

    public static readonly Error UserNotFound = new(
        "Auth.UserNotFound",
        "Usuário não encontrado.");

    public static readonly Error UserInactive = new(
        "Auth.UserInactive",
        "Usuário inativo.");

    public static readonly Error UserLocked = new(
        "Auth.UserLocked",
        "Usuário bloqueado temporariamente. Tente novamente mais tarde.");

    public static readonly Error InvalidCurrentPassword = new(
        "Auth.InvalidCurrentPassword",
        "Senha atual incorreta.");
}
