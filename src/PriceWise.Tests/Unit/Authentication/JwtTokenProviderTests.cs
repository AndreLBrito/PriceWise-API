using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using PriceWise.Infrastructure.Authentication;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Authentication;

public sealed class JwtTokenProviderTests
{
    [Fact]
    public void GenerateIncludesRoleClaim()
    {
        var provider = new JwtTokenProvider(new JwtOptions
        {
            Issuer = "PriceWise",
            Audience = "PriceWise",
            Secret = "pricewise-development-secret-key",
            AccessTokenExpirationMinutes = 60
        });
        var user = User.CreateAdmin("Admin", "admin@test.com", "hash");

        var accessToken = provider.Generate(user);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Value);
        token.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Role && claim.Value == "Admin");
    }
}
