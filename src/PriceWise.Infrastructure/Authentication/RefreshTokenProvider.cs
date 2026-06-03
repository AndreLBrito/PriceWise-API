using System.Security.Cryptography;
using System.Text;
using PriceWise.Application.Abstractions.Auth;

namespace PriceWise.Infrastructure.Authentication;

public sealed class RefreshTokenProvider : IRefreshTokenProvider
{
    private readonly JwtOptions options;

    public RefreshTokenProvider(JwtOptions options)
    {
        this.options = options;
    }

    public string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string Hash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public DateTime GetExpirationUtc()
    {
        return DateTime.UtcNow.AddDays(options.RefreshTokenExpirationDays);
    }
}
