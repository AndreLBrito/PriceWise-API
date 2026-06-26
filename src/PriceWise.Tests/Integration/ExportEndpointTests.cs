using System.Net;
using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class ExportEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public ExportEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("/api/v1/exports/products.csv")]
    [InlineData("/api/v1/exports/stores.csv")]
    [InlineData("/api/v1/exports/price-histories.csv")]
    [InlineData("/api/v1/exports/alert-notifications.csv")]
    public async Task ExportEndpointsRequireAuthentication(string url)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
