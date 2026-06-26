using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PriceWise.Api.Cors;
using PriceWise.Api.Extensions;

namespace PriceWise.Tests.Unit.Cors;

public sealed class CorsExtensionsTests
{
    [Fact]
    public async Task AddPriceWiseCorsRegistersConfiguredDevelopmentPolicy()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Cors:AllowedOrigins:1"] = "http://127.0.0.1:5173",
                ["Cors:AllowedMethods:0"] = "GET",
                ["Cors:AllowedMethods:1"] = "POST",
                ["Cors:AllowedMethods:2"] = "OPTIONS",
                ["Cors:AllowedHeaders:0"] = "Authorization",
                ["Cors:AllowedHeaders:1"] = "Content-Type",
                ["Cors:AllowedHeaders:2"] = "X-Correlation-Id"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddPriceWiseCors(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ApiCorsOptions>>().Value;
        var policyProvider = provider.GetRequiredService<ICorsPolicyProvider>();
        var policy = await policyProvider.GetPolicyAsync(
            new DefaultHttpContext(),
            ApiCorsPolicyNames.Development);

        options.AllowedOrigins.Should().HaveCount(2);
        policy.Should().NotBeNull();
        policy!.Origins.Should().BeEquivalentTo("http://localhost:5173", "http://127.0.0.1:5173");
        policy.Methods.Should().Contain(["GET", "POST", "OPTIONS"]);
        policy.Headers.Should().Contain(["Authorization", "Content-Type", "X-Correlation-Id"]);
        policy.AllowAnyOrigin.Should().BeFalse();
    }
}
