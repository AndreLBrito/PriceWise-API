using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class AlertNotificationEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public AlertNotificationEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/alert-notifications")]
    [InlineData("GET", "/api/alert-notifications/00000000-0000-0000-0000-000000000001")]
    public async Task AlertNotificationEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
