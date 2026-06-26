using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class CorsPreflightEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public CorsPreflightEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LoginPreflightReturnsCorsHeadersForDevelopmentWebOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle("http://localhost:5173");
        response.Headers.GetValues("Access-Control-Allow-Methods")
            .Should().ContainSingle(value => value.Contains("POST", StringComparison.Ordinal));
        response.Headers.GetValues("Access-Control-Allow-Headers")
            .Should().ContainSingle(value => value.Contains("Content-Type", StringComparison.OrdinalIgnoreCase));
    }
}
