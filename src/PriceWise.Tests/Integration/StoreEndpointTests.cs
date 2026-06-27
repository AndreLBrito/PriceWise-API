using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceWise.Application.Common;
using PriceWise.Application.Stores.Dtos;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class StoreEndpointTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public StoreEndpointTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task StoreListUsesSafeColumnsAndFallbackDirections()
    {
        var helper = new AuthenticatedHttpClient(factory.CreateClient());
        var auth = await helper.RegisterAsync();
        helper.Authenticate(auth.AccessToken);

        await CreateStoreAsync(helper.Client, "Alpha");
        await Task.Delay(10);
        await CreateStoreAsync(helper.Client, "Omega");

        var byCreatedAt = await ListStoresAsync(helper.Client, "createdAt", "desc");
        var byName = await ListStoresAsync(helper.Client, "name", "asc");
        var invalidSortBy = await ListStoresAsync(helper.Client, "colunaInexistente", "desc");
        var invalidDirection = await ListStoresAsync(helper.Client, "name", "invalida");

        byCreatedAt.Items.Select(store => store.Name).Should().Equal("Omega", "Alpha");
        byName.Items.Select(store => store.Name).Should().Equal("Alpha", "Omega");
        invalidSortBy.Items.Select(store => store.Name).Should().Equal("Omega", "Alpha");
        invalidDirection.Items.Select(store => store.Name).Should().Equal("Omega", "Alpha");
    }

    private static async Task CreateStoreAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/stores", new CreateStoreRequest(
            name,
            $"https://{name.ToLowerInvariant()}-{Guid.NewGuid():N}.example.com",
            null));

        response.EnsureSuccessStatusCode();
    }

    private static async Task<PagedResponse<StoreResponse>> ListStoresAsync(
        HttpClient client,
        string sortBy,
        string sortDirection)
    {
        var response = await client.GetAsync(
            $"/api/v1/stores?page=1&pageSize=20&sortBy={sortBy}&sortDirection={sortDirection}");
        response.EnsureSuccessStatusCode();

        return await AuthenticatedHttpClient.ReadDataAsync<PagedResponse<StoreResponse>>(response);
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
