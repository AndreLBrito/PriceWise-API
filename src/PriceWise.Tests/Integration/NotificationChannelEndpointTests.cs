using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class NotificationChannelEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public NotificationChannelEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/v1/notification-channels")]
    [InlineData("GET", "/api/v1/notification-channels/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/notification-channels")]
    [InlineData("PUT", "/api/v1/notification-channels/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/notification-channels/00000000-0000-0000-0000-000000000001")]
    public async Task NotificationChannelEndpointsRequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
