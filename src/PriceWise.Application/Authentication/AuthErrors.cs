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
}
