using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PriceWise.Application.Abstractions.DataSeeding;

namespace PriceWise.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class DataSeedTests
{
    private readonly PriceWiseWebApplicationFactory factory;

    public DataSeedTests(PriceWiseWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SeedCreatesDemoUser()
    {
        await factory.ResetDatabaseAsync();

        using var seededFactory = CreateSeededFactory();
        _ = seededFactory.CreateClient();

        var userCount = await CountAsync("users");
        var demoUserId = await ScalarAsync<Guid?>("select id from users where email = 'demo@pricewise.com'");

        userCount.Should().Be(1);
        demoUserId.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedCreatesProductsStoresAndPriceHistories()
    {
        await factory.ResetDatabaseAsync();

        using var seededFactory = CreateSeededFactory();
        _ = seededFactory.CreateClient();

        var productCount = await CountAsync("products");
        var storeCount = await CountAsync("stores");
        var priceHistoryCount = await CountAsync("price_histories");
        var activeAlertCount = await ScalarAsync<int>("select count(*)::int from price_alerts where is_active = true");
        var notificationCount = await CountAsync("alert_notifications");
        var channelCount = await CountAsync("notification_channels");

        productCount.Should().Be(5);
        storeCount.Should().Be(3);
        priceHistoryCount.Should().Be(15);
        activeAlertCount.Should().Be(3);
        notificationCount.Should().Be(2);
        channelCount.Should().Be(2);
    }

    [Fact]
    public async Task SeedDoesNotDuplicateDataWhenRunsAgain()
    {
        await factory.ResetDatabaseAsync();

        using var seededFactory = CreateSeededFactory();
        _ = seededFactory.CreateClient();
        using var scope = seededFactory.Services.CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();

        var firstCounts = await ReadCountsAsync();
        var result = await dataSeeder.SeedAsync();
        var secondCounts = await ReadCountsAsync();

        result.IsSuccess.Should().BeTrue();
        secondCounts.Should().BeEquivalentTo(firstCounts);
    }

    [Fact]
    public async Task SeedDoesNotRunWhenDisabled()
    {
        await factory.ResetDatabaseAsync();

        using var seededFactory = CreateSeededFactory(("DataSeed:Enabled", "false"));
        _ = seededFactory.CreateClient();

        var userCount = await CountAsync("users");

        userCount.Should().Be(0);
    }

    [Fact]
    public async Task SeedDoesNotRunInProduction()
    {
        await factory.ResetDatabaseAsync();

        using var seededFactory = CreateSeededFactory("Production", ("DataSeed:Enabled", "true"));
        _ = seededFactory.CreateClient();

        var userCount = await CountAsync("users");

        userCount.Should().Be(0);
    }

    private WebApplicationFactory<Program> CreateSeededFactory(
        params (string Key, string Value)[] overrides)
    {
        return CreateSeededFactory("Development", overrides);
    }

    private WebApplicationFactory<Program> CreateSeededFactory(
        string environment,
        params (string Key, string Value)[] overrides)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["DataSeed:Enabled"] = "true",
                    ["DataSeed:CreateDemoUser"] = "true",
                    ["DataSeed:DemoUserEmail"] = "demo@pricewise.com",
                    ["DataSeed:DemoUserPassword"] = "Demo@123456",
                    ["DataSeed:CreateDemoData"] = "true"
                };

                foreach (var (key, value) in overrides)
                {
                    values[key] = value;
                }

                configuration.AddInMemoryCollection(values);
            });
        });
    }

    private async Task<SeedCounts> ReadCountsAsync()
    {
        return new SeedCounts(
            await CountAsync("users"),
            await CountAsync("products"),
            await CountAsync("stores"),
            await CountAsync("price_histories"),
            await CountAsync("price_alerts"),
            await CountAsync("alert_notifications"),
            await CountAsync("notification_channels"));
    }

    private async Task<int> CountAsync(string table)
    {
        return await ScalarAsync<int>($"select count(*)::int from {table}");
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        return (await connection.ExecuteScalarAsync<T>(sql))!;
    }

    private sealed record SeedCounts(
        int Users,
        int Products,
        int Stores,
        int PriceHistories,
        int PriceAlerts,
        int AlertNotifications,
        int NotificationChannels);
}
