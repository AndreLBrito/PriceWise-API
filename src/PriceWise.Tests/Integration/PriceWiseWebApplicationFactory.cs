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
    private const string ExternalConnectionStringEnvironmentVariable = "PRICEWISE_TEST_POSTGRES_CONNECTION";

    private readonly PostgreSqlContainer? postgresContainer;
    private readonly string? externalConnectionString;

    public IntegrationTestFactory()
    {
        externalConnectionString = Environment.GetEnvironmentVariable(ExternalConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(externalConnectionString))
        {
            postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("pricewise_tests")
                .WithUsername("pricewise")
                .WithPassword("pricewise")
                .Build();
        }
    }

    public string ConnectionString => externalConnectionString ?? postgresContainer!.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (postgresContainer is not null)
        {
            await postgresContainer.StartAsync();
        }

        await WaitForDatabaseAsync();

        using var scope = Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        migrationRunner.MigrateUp();
    }

    public new async Task DisposeAsync()
    {
        if (postgresContainer is not null)
        {
            await postgresContainer.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        const string sql = """
            truncate table
                outbox_messages,
                audit_logs,
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

    private async Task WaitForDatabaseAsync()
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                return;
            }
            catch (NpgsqlException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        await using var finalConnection = new NpgsqlConnection(ConnectionString);
        await finalConnection.OpenAsync();
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
