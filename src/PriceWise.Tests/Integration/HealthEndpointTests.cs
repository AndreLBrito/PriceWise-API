using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PriceWise.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
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
        content.Should().Contain("Healthy");
    }
}
