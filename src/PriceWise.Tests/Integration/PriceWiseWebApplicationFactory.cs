using FluentMigrator.Runner;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace PriceWise.Tests.Integration;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pricewise_tests")
        .WithUsername("pricewise")
        .WithPassword("pricewise")
        .Build();

    public string ConnectionString => postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        using var scope = Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        migrationRunner.MigrateUp();
    }

    public new async Task DisposeAsync()
    {
        await postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        const string sql = """
            truncate table
                alert_notifications,
                notification_channels,
                price_check_executions,
                price_alerts,
                price_histories,
                products,
                stores,
                refresh_tokens,
                users
            restart identity cascade;
            """;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = ConnectionString,
                ["PriceCheck:Enabled"] = "false",
                ["Redis:Enabled"] = "false",
                ["Telemetry:Enabled"] = "false",
                ["RateLimiting:Enabled"] = "false",
                ["DataSeed:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();
        });
    }
}

public sealed class PriceWiseWebApplicationFactory : IntegrationTestFactory;
