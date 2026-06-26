using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class StoreEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public StoreEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/v1/stores")]
    [InlineData("GET", "/api/v1/stores/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/stores")]
    [InlineData("PUT", "/api/v1/stores/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/stores/00000000-0000-0000-0000-000000000001")]
    public async Task StoreEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
