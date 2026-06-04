using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceWise.Api.Common;
using PriceWise.Application.AlertNotifications.Dtos;
using PriceWise.Application.Dashboard.Dtos;
using PriceWise.Application.NotificationChannels.Dtos;
using PriceWise.Application.PriceHistories.Dtos;
using PriceWise.Application.Products.Dtos;
using PriceWise.Application.Stores.Dtos;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class EndToEndFlowTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public EndToEndFlowTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthCheckReturnsSuccess()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticationFlowRegistersLogsInRefreshesAndLogsOut()
    {
        await factory.ResetDatabaseAsync();
        var helper = new AuthenticatedHttpClient(factory.CreateClient());
        var email = AuthenticatedHttpClient.UniqueEmail();

        var register = await helper.RegisterAsync(email);
        var login = await helper.LoginAsync(email);
        var refresh = await helper.RefreshTokenAsync(login.RefreshToken);
        var logout = await helper.LogoutAsync(refresh.RefreshToken);
        var reuseRefresh = await helper.Client.PostAsJsonAsync(
            "/api/auth/refresh-token",
            new { refreshToken = refresh.RefreshToken });

        register.UserId.Should().NotBeEmpty();
        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refresh.AccessToken.Should().NotBe(login.AccessToken);
        logout.IsSuccessStatusCode.Should().BeTrue();
        reuseRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProductFlowCreatesListsGetsUpdatesAndDeletesProduct()
    {
        await factory.ResetDatabaseAsync();
        var helper = await CreateAuthenticatedHelperAsync();

        var created = await helper.CreateProductAsync();
        var list = await GetAsync<IReadOnlyCollection<ProductResponse>>(helper.Client, "/api/products");
        var get = await GetAsync<ProductResponse>(helper.Client, $"/api/products/{created.Id}");
        var updateResponse = await helper.Client.PutAsJsonAsync($"/api/products/{created.Id}", new
        {
            name = "Notebook Atualizado",
            description = "Produto atualizado",
            brand = "PriceWise",
            category = "Eletronicos",
            productUrl = created.ProductUrl,
            imageUrl = created.ImageUrl
        });
        var updated = await AuthenticatedHttpClient.ReadDataAsync<ProductResponse>(updateResponse);
        var deleteResponse = await helper.Client.DeleteAsync($"/api/products/{created.Id}");
        var getDeleted = await helper.Client.GetAsync($"/api/products/{created.Id}");
        var unauthenticated = await factory.CreateClient().GetAsync("/api/products");

        list.Should().ContainSingle(product => product.Id == created.Id);
        get.Id.Should().Be(created.Id);
        updated.Name.Should().Be("Notebook Atualizado");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getDeleted.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unauthenticated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StoreFlowCreatesListsGetsUpdatesAndDeletesStore()
    {
        await factory.ResetDatabaseAsync();
        var helper = await CreateAuthenticatedHelperAsync();

        var created = await helper.CreateStoreAsync();
        var list = await GetAsync<IReadOnlyCollection<StoreResponse>>(helper.Client, "/api/stores");
        var get = await GetAsync<StoreResponse>(helper.Client, $"/api/stores/{created.Id}");
        var updateResponse = await helper.Client.PutAsJsonAsync($"/api/stores/{created.Id}", new
        {
            name = "Loja Atualizada",
            baseUrl = created.BaseUrl,
            logoUrl = created.LogoUrl
        });
        var updated = await AuthenticatedHttpClient.ReadDataAsync<StoreResponse>(updateResponse);
        var deleteResponse = await helper.Client.DeleteAsync($"/api/stores/{created.Id}");
        var getDeleted = await helper.Client.GetAsync($"/api/stores/{created.Id}");
        var unauthenticated = await factory.CreateClient().GetAsync("/api/stores");

        list.Should().ContainSingle(store => store.Id == created.Id);
        get.Id.Should().Be(created.Id);
        updated.Name.Should().Be("Loja Atualizada");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getDeleted.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unauthenticated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PriceHistoryFlowCreatesAndCalculatesSummaries()
    {
        await factory.ResetDatabaseAsync();
        var helper = await CreateAuthenticatedHelperAsync();
        var product = await helper.CreateProductAsync();
        var store = await helper.CreateStoreAsync();

        await helper.CreatePriceHistoryAsync(product.Id, store.Id, 100, DateTime.UtcNow.AddMinutes(-2));
        await helper.CreatePriceHistoryAsync(product.Id, store.Id, 80, DateTime.UtcNow.AddMinutes(-1));

        var latest = await GetAsync<PriceHistoryResponse>(
            helper.Client,
            $"/api/products/{product.Id}/price-histories/latest");
        var lowest = await GetAsync<PriceHistoryResponse>(
            helper.Client,
            $"/api/products/{product.Id}/price-histories/lowest");
        var average = await GetAsync<AveragePriceHistoryResponse>(
            helper.Client,
            $"/api/products/{product.Id}/price-histories/average");

        latest.Price.Should().Be(80);
        lowest.Price.Should().Be(80);
        average.AveragePrice.Should().Be(90);
        average.EntriesCount.Should().Be(2);
    }

    [Fact]
    public async Task PriceHistoryCannotUseProductFromAnotherUser()
    {
        await factory.ResetDatabaseAsync();
        var firstUser = await CreateAuthenticatedHelperAsync();
        var secondUser = await CreateAuthenticatedHelperAsync();
        var productFromFirstUser = await firstUser.CreateProductAsync();
        var storeFromSecondUser = await secondUser.CreateStoreAsync();

        var response = await secondUser.Client.PostAsJsonAsync("/api/price-histories", new
        {
            productId = productFromFirstUser.Id,
            storeId = storeFromSecondUser.Id,
            price = 90,
            currency = "BRL",
            capturedAt = DateTime.UtcNow,
            sourceUrl = "https://example.com/source"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PriceAlertCreatesSingleAlertNotificationWhenPriceReachesTarget()
    {
        await factory.ResetDatabaseAsync();
        var helper = await CreateAuthenticatedHelperAsync();
        var product = await helper.CreateProductAsync();
        var store = await helper.CreateStoreAsync();
        var alert = await helper.CreatePriceAlertAsync(product.Id, 100);

        await helper.CreatePriceHistoryAsync(product.Id, store.Id, 90);
        var notifications = await GetAsync<IReadOnlyCollection<AlertNotificationResponse>>(
            helper.Client,
            "/api/alert-notifications");
        var duplicateHistoryIdResponse = await helper.Client.PostAsJsonAsync("/api/price-histories", new
        {
            productId = product.Id,
            storeId = store.Id,
            price = 90,
            currency = "BRL",
            capturedAt = DateTime.UtcNow,
            sourceUrl = "https://example.com/source"
        });
        duplicateHistoryIdResponse.EnsureSuccessStatusCode();
        var notificationsAfterSecondHistory = await GetAsync<IReadOnlyCollection<AlertNotificationResponse>>(
            helper.Client,
            "/api/alert-notifications");

        notifications.Should().ContainSingle(notification =>
            notification.PriceAlertId == alert.Id
            && notification.TriggeredPrice == 90
            && notification.TargetPrice == 100);
        notificationsAfterSecondHistory.Should().HaveCount(2);
        notificationsAfterSecondHistory.Select(notification => notification.PriceHistoryId)
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task DashboardSummaryReturnsCoherentData()
    {
        await factory.ResetDatabaseAsync();
        var helper = await CreateAuthenticatedHelperAsync();
        var product = await helper.CreateProductAsync();
        var store = await helper.CreateStoreAsync();
        await helper.CreatePriceAlertAsync(product.Id, 100);
        await helper.CreatePriceHistoryAsync(product.Id, store.Id, 90);

        var summary = await GetAsync<DashboardSummaryResponse>(helper.Client, "/api/dashboard/summary");

        summary.TotalProducts.Should().Be(1);
        summary.ActiveProducts.Should().Be(1);
        summary.TotalStores.Should().Be(1);
        summary.ActiveStores.Should().Be(1);
        summary.TotalPriceAlerts.Should().Be(1);
        summary.ActivePriceAlerts.Should().Be(1);
        summary.TotalPriceHistories.Should().Be(1);
        summary.TotalAlertNotifications.Should().Be(1);
        summary.LowestPriceRegistered.Should().Be(90);
        summary.HighestPriceRegistered.Should().Be(90);
    }

    [Fact]
    public async Task NotificationChannelFlowValidatesCreatesAndDeletesChannels()
    {
        await factory.ResetDatabaseAsync();
        var helper = await CreateAuthenticatedHelperAsync();

        var webhook = await helper.CreateNotificationChannelAsync(
            "Webhook",
            "Webhook",
            "https://example.com/webhook");
        var email = await helper.CreateNotificationChannelAsync(
            "Email",
            "Email",
            "user@example.com");
        var invalidWebhook = await helper.Client.PostAsJsonAsync("/api/notification-channels", new
        {
            type = "Webhook",
            name = "Webhook invalido",
            destination = "invalid-url"
        });
        var invalidEmail = await helper.Client.PostAsJsonAsync("/api/notification-channels", new
        {
            type = "Email",
            name = "Email invalido",
            destination = "invalid-email"
        });
        var deleteResponse = await helper.Client.DeleteAsync($"/api/notification-channels/{webhook.Id}");
        var getDeleted = await helper.Client.GetAsync($"/api/notification-channels/{webhook.Id}");

        webhook.Type.Should().Be("Webhook");
        email.Type.Should().Be("Email");
        invalidWebhook.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalidEmail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getDeleted.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AuthenticatedHttpClient> CreateAuthenticatedHelperAsync()
    {
        var helper = new AuthenticatedHttpClient(factory.CreateClient());
        var auth = await helper.RegisterAsync();
        helper.Authenticate(auth.AccessToken);

        return helper;
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await AuthenticatedHttpClient.ReadDataAsync<T>(response);
    }
}
