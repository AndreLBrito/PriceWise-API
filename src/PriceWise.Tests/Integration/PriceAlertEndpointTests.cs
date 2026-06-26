using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class PriceAlertEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public PriceAlertEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/v1/price-alerts")]
    [InlineData("GET", "/api/v1/price-alerts/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/price-alerts")]
    [InlineData("PUT", "/api/v1/price-alerts/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/price-alerts/00000000-0000-0000-0000-000000000001")]
    public async Task PriceAlertEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
