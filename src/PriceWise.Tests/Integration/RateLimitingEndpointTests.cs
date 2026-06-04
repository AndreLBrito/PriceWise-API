using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Authentication;
using PriceWise.Application.Authentication.Dtos;
using PriceWise.Application.Common;
using PriceWise.Application.PriceChecks;
using PriceWise.Application.PriceChecks.Dtos;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class RateLimitingEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public RateLimitingEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LoginEndpointReturnsTooManyRequestsAfterLimitIsExceeded()
    {
        var client = CreateClientWithRateLimiting(new Dictionary<string, string?>
        {
            ["RateLimiting:LoginPermitLimit"] = "2",
            ["RateLimiting:LoginWindowInMinutes"] = "1"
        }, replaceServices: true);

        await PostLoginAsync(client);
        await PostLoginAsync(client);

        var response = await PostLoginAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GeneralEndpointReturnsTooManyRequestsAfterLimitIsExceeded()
    {
        var client = CreateClientWithRateLimiting(new Dictionary<string, string?>
        {
            ["RateLimiting:GeneralPermitLimit"] = "1",
            ["RateLimiting:GeneralWindowInMinutes"] = "1"
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(Guid.NewGuid()));

        await client.GetAsync("/api/telemetry/info");

        var response = await client.GetAsync("/api/telemetry/info");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ManualPriceCheckEndpointReturnsTooManyRequestsAfterLimitIsExceeded()
    {
        var client = CreateClientWithRateLimiting(new Dictionary<string, string?>
        {
            ["RateLimiting:PriceCheckPermitLimit"] = "1",
            ["RateLimiting:PriceCheckWindowInMinutes"] = "5"
        }, replaceServices: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(Guid.NewGuid()));

        await client.PostAsync("/api/price-check/run", null);

        var response = await client.PostAsync("/api/price-check/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task DisabledRateLimitingDoesNotBlockRequests()
    {
        var client = CreateClientWithRateLimiting(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "false",
            ["RateLimiting:LoginPermitLimit"] = "1"
        }, replaceServices: true);

        await PostLoginAsync(client);
        await PostLoginAsync(client);

        var response = await PostLoginAsync(client);

        response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client)
    {
        return client.PostAsync("/api/auth/login", JsonContent.Create(new
        {
            email = "user@example.com",
            password = "wrong-password"
        }));
    }

    private HttpClient CreateClientWithRateLimiting(
        Dictionary<string, string?> configuration,
        bool replaceServices = false)
    {
        var testConfiguration = new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:LoginPermitLimit"] = "100",
            ["RateLimiting:RefreshTokenPermitLimit"] = "100",
            ["RateLimiting:GeneralPermitLimit"] = "100",
            ["RateLimiting:PriceCheckPermitLimit"] = "100"
        };

        foreach (var item in configuration)
        {
            testConfiguration[item.Key] = item.Value;
        }

        return factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(testConfiguration);
                });

                if (replaceServices)
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IAuthService>();
                        services.RemoveAll<IPriceCheckService>();
                        services.AddScoped<IAuthService, FakeAuthService>();
                        services.AddScoped<IPriceCheckService, FakePriceCheckService>();
                    });
                }
            })
            .CreateClient();
    }

    private static string CreateToken(Guid userId)
    {
        const string secret = "pricewise-development-secret-key";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "PriceWise",
            audience: "PriceWise",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AuthResponse>.Success(CreateResponse()));
        }

        public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AuthResponse>.Success(CreateResponse()));
        }

        public Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AuthResponse>.Success(CreateResponse()));
        }

        public Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result<CurrentUserResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CurrentUserResponse>.Success(new CurrentUserResponse(
                userId,
                "Usuário Teste",
                "user@example.com",
                "User",
                DateTime.UtcNow)));
        }

        public Task<Result> ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> RevokeRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }

        private static AuthResponse CreateResponse()
        {
            return new AuthResponse(
                Guid.NewGuid(),
                "Usuário Teste",
                "user@example.com",
                "User",
                "access-token",
                "refresh-token",
                DateTime.UtcNow.AddMinutes(10));
        }
    }

    private sealed class FakePriceCheckService : IPriceCheckService
    {
        public Task<Result<PriceCheckRunResponse>> RunAsync(CancellationToken cancellationToken = default)
        {
            return RunAsync(PriceCheckTrigger.Manual, cancellationToken);
        }

        public Task<Result<PriceCheckRunResponse>> RunAsync(
            PriceCheckTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<PriceCheckRunResponse>.Success(new PriceCheckRunResponse(
                DateTime.UtcNow,
                "Concluída",
                "Verificação concluída.",
                0,
                0,
                0,
                0)));
        }

        public Task<Result<PriceCheckStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<PriceCheckStatusResponse>.Success(new PriceCheckStatusResponse(
                true,
                30,
                50,
                null,
                null,
                null)));
        }
    }
}
