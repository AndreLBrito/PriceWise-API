using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class PriceHistoryEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public PriceHistoryEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("POST", "/api/v1/price-histories")]
    [InlineData("GET", "/api/v1/products/00000000-0000-0000-0000-000000000001/price-histories")]
    [InlineData("GET", "/api/v1/products/00000000-0000-0000-0000-000000000001/price-histories/latest")]
    [InlineData("GET", "/api/v1/products/00000000-0000-0000-0000-000000000001/price-histories/lowest")]
    [InlineData("GET", "/api/v1/products/00000000-0000-0000-0000-000000000001/price-histories/average")]
    public async Task PriceHistoryEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
