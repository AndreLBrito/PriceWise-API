using FluentAssertions;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class HealthEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public HealthEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetHealthReturnsHealthyStatus()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Saudável");
    }
}
