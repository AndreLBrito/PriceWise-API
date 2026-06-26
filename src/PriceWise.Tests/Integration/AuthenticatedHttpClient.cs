using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PriceWise.Api.Common;
using PriceWise.Application.Authentication.Dtos;
using PriceWise.Application.NotificationChannels.Dtos;
using PriceWise.Application.PriceAlerts.Dtos;
using PriceWise.Application.PriceHistories.Dtos;
using PriceWise.Application.Products.Dtos;
using PriceWise.Application.Stores.Dtos;

namespace PriceWise.Tests.Integration;

public sealed class AuthenticatedHttpClient
{
    private readonly HttpClient client;

    public AuthenticatedHttpClient(HttpClient client)
    {
        this.client = client;
    }

    public HttpClient Client => client;

    public async Task<AuthResponse> RegisterAsync(string? email = null, string password = "Password123!")
    {
        var request = new RegisterRequest(
            "Integration User",
            email ?? UniqueEmail(),
            password);

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<AuthResponse>(response);
    }

    public async Task<AuthResponse> LoginAsync(string email, string password = "Password123!")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<AuthResponse>(response);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh-token", new RefreshTokenRequest(refreshToken));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<AuthResponse>(response);
    }

    public async Task<HttpResponseMessage> LogoutAsync(string refreshToken)
    {
        return await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(refreshToken));
    }

    public void Authenticate(string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<ProductResponse> CreateProductAsync(string? productUrl = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(
            "Notebook",
            "Notebook para testes",
            "PriceWise",
            "Eletronicos",
            productUrl ?? $"https://example.com/products/{Guid.NewGuid():N}",
            "https://example.com/image.png"));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<ProductResponse>(response);
    }

    public async Task<StoreResponse> CreateStoreAsync(string? baseUrl = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/stores", new CreateStoreRequest(
            "Loja Teste",
            baseUrl ?? $"https://store-{Guid.NewGuid():N}.example.com",
            "https://example.com/logo.png"));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<StoreResponse>(response);
    }

    public async Task<PriceHistoryResponse> CreatePriceHistoryAsync(
        Guid productId,
        Guid storeId,
        decimal price,
        DateTime? capturedAt = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/price-histories", new CreatePriceHistoryRequest(
            productId,
            storeId,
            price,
            "BRL",
            capturedAt,
            "https://example.com/source"));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<PriceHistoryResponse>(response);
    }

    public async Task<PriceAlertResponse> CreatePriceAlertAsync(Guid productId, decimal targetPrice)
    {
        var response = await client.PostAsJsonAsync("/api/v1/price-alerts", new CreatePriceAlertRequest(
            productId,
            targetPrice));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<PriceAlertResponse>(response);
    }

    public async Task<NotificationChannelResponse> CreateNotificationChannelAsync(
        string type,
        string name,
        string destination)
    {
        var response = await client.PostAsJsonAsync("/api/v1/notification-channels", new CreateNotificationChannelRequest(
            type,
            name,
            destination));
        response.EnsureSuccessStatusCode();

        return await ReadDataAsync<NotificationChannelResponse>(response);
    }

    public static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();

        return apiResponse.Data!;
    }

    public static string UniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@example.com";
    }
}
