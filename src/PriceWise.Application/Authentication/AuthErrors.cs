using PriceWise.Application.Common;

namespace PriceWise.Application.Authentication;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyRegistered = new(
        "Auth.EmailAlreadyRegistered",
        "Email is already registered.");

    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly Error InvalidRefreshToken = new(
        "Auth.InvalidRefreshToken",
        "Invalid refresh token.");
}
