namespace PriceWise.Application.Abstractions.Auth;

public interface IRefreshTokenProvider
{
    string Generate();

    string Hash(string refreshToken);

    DateTime GetExpirationUtc();
}
