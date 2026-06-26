using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public DashboardEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("/api/v1/dashboard/summary")]
    [InlineData("/api/v1/dashboard/products/00000000-0000-0000-0000-000000000001/summary")]
    [InlineData("/api/v1/dashboard/stores/00000000-0000-0000-0000-000000000001/summary")]
    [InlineData("/api/v1/dashboard/alerts/summary")]
    public async Task DashboardEndpointsRequireAuthentication(string url)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
