using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public ProductEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/v1/products")]
    [InlineData("GET", "/api/v1/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/products")]
    [InlineData("PUT", "/api/v1/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/products/00000000-0000-0000-0000-000000000001")]
    public async Task ProductEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
