using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class PriceCheckEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public PriceCheckEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("POST", "/api/v1/price-check/run")]
    [InlineData("GET", "/api/v1/price-check/status")]
    public async Task PriceCheckEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
