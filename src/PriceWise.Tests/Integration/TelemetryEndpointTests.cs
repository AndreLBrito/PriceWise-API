using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class TelemetryEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public TelemetryEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task TelemetryInfoEndpointRequiresAuthentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/telemetry/info");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
