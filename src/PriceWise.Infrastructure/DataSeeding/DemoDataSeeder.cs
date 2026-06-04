using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Auth;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.DataSeeding;
using PriceWise.Application.Common;

namespace PriceWise.Infrastructure.DataSeeding;

public sealed class DemoDataSeeder : IDataSeeder
{
    private const string CreatedBy = "demo-seed";
    private static readonly Guid DefaultDemoUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid DefaultAdminUserId = Guid.Parse("10000000-0000-0000-0000-000000000099");

    private readonly IDbConnectionFactory connectionFactory;
    private readonly IPasswordHasher passwordHasher;
    private readonly IOptions<DataSeedOptions> options;
    private readonly IOptions<AdminSeedOptions> adminOptions;
    private readonly IHostEnvironment environment;
    private readonly ILogger<DemoDataSeeder> logger;

    public DemoDataSeeder(
        IDbConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        IOptions<DataSeedOptions> options,
        IOptions<AdminSeedOptions> adminOptions,
        IHostEnvironment environment,
        ILogger<DemoDataSeeder> logger)
    {
        this.connectionFactory = connectionFactory;
        this.passwordHasher = passwordHasher;
        this.options = options;
        this.adminOptions = adminOptions;
        this.environment = environment;
        this.logger = logger;
    }

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        var seedOptions = options.Value;
        var adminSeedOptions = adminOptions.Value;

        if (environment.IsProduction())
        {
            logger.LogWarning("Seed não será executado em Production.");
            return Result.Failure(new Error("DataSeed.Production", "Seed não pode ser executado em Production."));
        }

        logger.LogInformation("Iniciando seed de segurança e dados de demonstração.");

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            await SeedAdminAsync(connection, transaction, adminSeedOptions);

            if (!seedOptions.Enabled)
            {
                logger.LogInformation("Seed de demonstração está desabilitado.");
                transaction.Commit();
                return Result.Success();
            }

            var userId = await SeedUserAsync(connection, transaction, seedOptions);
            if (userId is not null && seedOptions.CreateDemoData)
            {
                await SeedProductsAsync(connection, transaction, userId.Value);
                await SeedStoresAsync(connection, transaction, userId.Value);
                await SeedPriceHistoriesAsync(connection, transaction, userId.Value);
                await SeedPriceAlertsAsync(connection, transaction, userId.Value);
                await SeedAlertNotificationsAsync(connection, transaction, userId.Value);
                await SeedNotificationChannelsAsync(connection, transaction, userId.Value);
            }

            transaction.Commit();
            logger.LogInformation("Seed concluído com sucesso.");

            return Result.Success();
        }
        catch (Exception exception)
        {
            transaction.Rollback();
            logger.LogError(exception, "Falha ao executar seed.");

            return Result.Failure(new Error("DataSeed.Failed", "Não foi possível executar o seed."));
        }
    }

    private async Task SeedAdminAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        AdminSeedOptions seedOptions)
    {
        if (!seedOptions.Enabled)
        {
            logger.LogInformation("Seed de usuário administrador está desabilitado.");
            return;
        }

        const string insertSql = """
            insert into users (
                id, name, email, password_hash, role, is_active,
                failed_login_attempts, locked_until_utc, created_at_utc, created_by)
            select
                @Id, @Name, @Email, @PasswordHash, 'Admin', true,
                0, null, @CreatedAtUtc, @CreatedBy
            where not exists (select 1 from users where email = @Email);
            """;

        await connection.ExecuteAsync(
            insertSql,
            new
            {
                Id = DefaultAdminUserId,
                Name = "Administrador PriceWise",
                Email = seedOptions.Email.ToLowerInvariant(),
                PasswordHash = passwordHasher.Hash(seedOptions.Password),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy
            },
            transaction);
    }

    private async Task<Guid?> SeedUserAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        DataSeedOptions seedOptions)
    {
        if (!seedOptions.CreateDemoUser)
        {
            return await connection.ExecuteScalarAsync<Guid?>(
                "select id from users where email = @Email",
                new { Email = seedOptions.DemoUserEmail.ToLowerInvariant() },
                transaction);
        }

        const string insertSql = """
                insert into users (
                    id, name, email, password_hash, role, is_active,
                    failed_login_attempts, locked_until_utc, created_at_utc, created_by)
                select
                    @Id, @Name, @Email, @PasswordHash, 'User', true,
                    0, null, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from users where email = @Email);
                """;

        await connection.ExecuteAsync(
            insertSql,
            new
            {
                Id = DefaultDemoUserId,
                Name = "Usuário Demonstração",
                Email = seedOptions.DemoUserEmail.ToLowerInvariant(),
                PasswordHash = passwordHasher.Hash(seedOptions.DemoUserPassword),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy
            },
            transaction);

        return await connection.ExecuteScalarAsync<Guid?>(
            "select id from users where email = @Email",
            new { Email = seedOptions.DemoUserEmail.ToLowerInvariant() },
            transaction);
    }

    private static async Task SeedProductsAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid userId)
    {
        foreach (var product in DemoProducts)
        {
            const string sql = """
                insert into products (
                    id, user_id, name, description, brand, category, product_url, image_url,
                    is_active, created_at_utc, created_by)
                select
                    @Id, @UserId, @Name, @Description, @Brand, @Category, @ProductUrl, @ImageUrl,
                    @IsActive, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from products where id = @Id);
                """;

            await connection.ExecuteAsync(sql, product with { UserId = userId }, transaction);
        }
    }

    private static async Task SeedStoresAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid userId)
    {
        foreach (var store in DemoStores)
        {
            const string sql = """
                insert into stores (
                    id, user_id, name, base_url, logo_url, is_active, created_at_utc, created_by)
                select
                    @Id, @UserId, @Name, @BaseUrl, @LogoUrl, true, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from stores where id = @Id);
                """;

            await connection.ExecuteAsync(sql, store with { UserId = userId }, transaction);
        }
    }

    private static async Task SeedPriceHistoriesAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid userId)
    {
        foreach (var history in DemoPriceHistories)
        {
            const string sql = """
                insert into price_histories (
                    id, user_id, product_id, store_id, price, currency, captured_at,
                    source_url, created_at_utc, created_by)
                select
                    @Id, @UserId, @ProductId, @StoreId, @Price, 'BRL', @CapturedAt,
                    @SourceUrl, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from price_histories where id = @Id);
                """;

            await connection.ExecuteAsync(sql, history with { UserId = userId }, transaction);
        }
    }

    private static async Task SeedPriceAlertsAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid userId)
    {
        foreach (var alert in DemoPriceAlerts)
        {
            const string sql = """
                insert into price_alerts (
                    id, user_id, product_id, target_price, is_active, created_at_utc, created_by)
                select
                    @Id, @UserId, @ProductId, @TargetPrice, @IsActive, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from price_alerts where id = @Id);
                """;

            await connection.ExecuteAsync(sql, alert with { UserId = userId }, transaction);
        }
    }

    private static async Task SeedAlertNotificationsAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid userId)
    {
        foreach (var notification in DemoAlertNotifications)
        {
            const string sql = """
                insert into alert_notifications (
                    id, user_id, price_alert_id, product_id, price_history_id, triggered_price,
                    target_price, triggered_at, created_at_utc, created_by)
                select
                    @Id, @UserId, @PriceAlertId, @ProductId, @PriceHistoryId, @TriggeredPrice,
                    @TargetPrice, @TriggeredAt, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from alert_notifications where id = @Id);
                """;

            await connection.ExecuteAsync(sql, notification with { UserId = userId }, transaction);
        }
    }

    private static async Task SeedNotificationChannelsAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid userId)
    {
        foreach (var channel in DemoNotificationChannels)
        {
            const string sql = """
                insert into notification_channels (
                    id, user_id, type, name, destination, is_active, created_at_utc, created_by)
                select
                    @Id, @UserId, @Type, @Name, @Destination, true, @CreatedAtUtc, @CreatedBy
                where not exists (select 1 from notification_channels where id = @Id);
                """;

            await connection.ExecuteAsync(sql, channel with { UserId = userId }, transaction);
        }
    }

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.Date.AddDays(-days).AddHours(14);

    private static readonly ProductSeed[] DemoProducts =
    [
        new(Guid.Parse("20000000-0000-0000-0000-000000000001"), Guid.Empty, "Notebook Dell Inspiron 15", "Notebook para trabalho e estudos com SSD e 16GB RAM.", "Dell", "Notebook", "https://demo.pricewise.com/products/notebook-dell-inspiron-15", "https://demo.pricewise.com/images/notebook-dell.png", true, DaysAgo(29), CreatedBy),
        new(Guid.Parse("20000000-0000-0000-0000-000000000002"), Guid.Empty, "Smartphone Samsung Galaxy S25", "Smartphone Android premium com 256GB.", "Samsung", "Smartphone", "https://demo.pricewise.com/products/samsung-galaxy-s25", "https://demo.pricewise.com/images/galaxy-s25.png", true, DaysAgo(28), CreatedBy),
        new(Guid.Parse("20000000-0000-0000-0000-000000000003"), Guid.Empty, "Monitor LG UltraWide 29", "Monitor ultrawide para produtividade.", "LG", "Monitor", "https://demo.pricewise.com/products/lg-ultrawide-29", "https://demo.pricewise.com/images/lg-ultrawide.png", true, DaysAgo(27), CreatedBy),
        new(Guid.Parse("20000000-0000-0000-0000-000000000004"), Guid.Empty, "Fone Sony WH-1000XM5", "Fone Bluetooth com cancelamento de ruído.", "Sony", "Audio", "https://demo.pricewise.com/products/sony-wh-1000xm5", "https://demo.pricewise.com/images/sony-wh1000xm5.png", true, DaysAgo(26), CreatedBy),
        new(Guid.Parse("20000000-0000-0000-0000-000000000005"), Guid.Empty, "Cafeteira Nespresso Vertuo", "Cafeteira compacta para cápsulas.", "Nespresso", "Casa", "https://demo.pricewise.com/products/nespresso-vertuo", "https://demo.pricewise.com/images/nespresso-vertuo.png", true, DaysAgo(25), CreatedBy)
    ];

    private static readonly StoreSeed[] DemoStores =
    [
        new(Guid.Parse("30000000-0000-0000-0000-000000000001"), Guid.Empty, "Amazon Brasil", "https://www.amazon.com.br", "https://demo.pricewise.com/logos/amazon.png", DaysAgo(29), CreatedBy),
        new(Guid.Parse("30000000-0000-0000-0000-000000000002"), Guid.Empty, "Magazine Luiza", "https://www.magazineluiza.com.br", "https://demo.pricewise.com/logos/magalu.png", DaysAgo(29), CreatedBy),
        new(Guid.Parse("30000000-0000-0000-0000-000000000003"), Guid.Empty, "Kabum", "https://www.kabum.com.br", "https://demo.pricewise.com/logos/kabum.png", DaysAgo(29), CreatedBy)
    ];

    private static readonly PriceHistorySeed[] DemoPriceHistories =
    [
        new(Guid.Parse("40000000-0000-0000-0000-000000000001"), Guid.Empty, DemoProducts[0].Id, DemoStores[0].Id, 4899.90m, DaysAgo(29), "https://www.amazon.com.br/notebook-dell", DaysAgo(29), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000002"), Guid.Empty, DemoProducts[0].Id, DemoStores[1].Id, 4699.90m, DaysAgo(21), "https://www.magazineluiza.com.br/notebook-dell", DaysAgo(21), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000003"), Guid.Empty, DemoProducts[0].Id, DemoStores[2].Id, 4299.90m, DaysAgo(6), "https://www.kabum.com.br/notebook-dell", DaysAgo(6), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000004"), Guid.Empty, DemoProducts[1].Id, DemoStores[0].Id, 3899.00m, DaysAgo(26), "https://www.amazon.com.br/galaxy-s25", DaysAgo(26), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000005"), Guid.Empty, DemoProducts[1].Id, DemoStores[1].Id, 3599.00m, DaysAgo(16), "https://www.magazineluiza.com.br/galaxy-s25", DaysAgo(16), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000006"), Guid.Empty, DemoProducts[1].Id, DemoStores[2].Id, 3299.00m, DaysAgo(3), "https://www.kabum.com.br/galaxy-s25", DaysAgo(3), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000007"), Guid.Empty, DemoProducts[2].Id, DemoStores[0].Id, 1199.90m, DaysAgo(24), "https://www.amazon.com.br/lg-ultrawide", DaysAgo(24), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000008"), Guid.Empty, DemoProducts[2].Id, DemoStores[1].Id, 1099.90m, DaysAgo(12), "https://www.magazineluiza.com.br/lg-ultrawide", DaysAgo(12), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000009"), Guid.Empty, DemoProducts[2].Id, DemoStores[2].Id, 999.90m, DaysAgo(2), "https://www.kabum.com.br/lg-ultrawide", DaysAgo(2), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000010"), Guid.Empty, DemoProducts[3].Id, DemoStores[0].Id, 2299.90m, DaysAgo(20), "https://www.amazon.com.br/sony-wh1000xm5", DaysAgo(20), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000011"), Guid.Empty, DemoProducts[3].Id, DemoStores[1].Id, 1999.90m, DaysAgo(10), "https://www.magazineluiza.com.br/sony-wh1000xm5", DaysAgo(10), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000012"), Guid.Empty, DemoProducts[3].Id, DemoStores[2].Id, 1799.90m, DaysAgo(1), "https://www.kabum.com.br/sony-wh1000xm5", DaysAgo(1), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000013"), Guid.Empty, DemoProducts[4].Id, DemoStores[0].Id, 799.90m, DaysAgo(18), "https://www.amazon.com.br/nespresso-vertuo", DaysAgo(18), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000014"), Guid.Empty, DemoProducts[4].Id, DemoStores[1].Id, 749.90m, DaysAgo(8), "https://www.magazineluiza.com.br/nespresso-vertuo", DaysAgo(8), CreatedBy),
        new(Guid.Parse("40000000-0000-0000-0000-000000000015"), Guid.Empty, DemoProducts[4].Id, DemoStores[2].Id, 699.90m, DaysAgo(4), "https://www.kabum.com.br/nespresso-vertuo", DaysAgo(4), CreatedBy)
    ];

    private static readonly PriceAlertSeed[] DemoPriceAlerts =
    [
        new(Guid.Parse("50000000-0000-0000-0000-000000000001"), Guid.Empty, DemoProducts[0].Id, 4300.00m, true, DaysAgo(20), CreatedBy),
        new(Guid.Parse("50000000-0000-0000-0000-000000000002"), Guid.Empty, DemoProducts[1].Id, 3000.00m, true, DaysAgo(15), CreatedBy),
        new(Guid.Parse("50000000-0000-0000-0000-000000000003"), Guid.Empty, DemoProducts[2].Id, 950.00m, false, DaysAgo(14), CreatedBy),
        new(Guid.Parse("50000000-0000-0000-0000-000000000004"), Guid.Empty, DemoProducts[3].Id, 1800.00m, true, DaysAgo(9), CreatedBy),
        new(Guid.Parse("50000000-0000-0000-0000-000000000005"), Guid.Empty, DemoProducts[4].Id, 650.00m, false, DaysAgo(7), CreatedBy)
    ];

    private static readonly AlertNotificationSeed[] DemoAlertNotifications =
    [
        new(Guid.Parse("60000000-0000-0000-0000-000000000001"), Guid.Empty, DemoPriceAlerts[0].Id, DemoProducts[0].Id, DemoPriceHistories[2].Id, 4299.90m, 4300.00m, DaysAgo(6), DaysAgo(6), CreatedBy),
        new(Guid.Parse("60000000-0000-0000-0000-000000000002"), Guid.Empty, DemoPriceAlerts[3].Id, DemoProducts[3].Id, DemoPriceHistories[11].Id, 1799.90m, 1800.00m, DaysAgo(1), DaysAgo(1), CreatedBy)
    ];

    private static readonly NotificationChannelSeed[] DemoNotificationChannels =
    [
        new(Guid.Parse("70000000-0000-0000-0000-000000000001"), Guid.Empty, "Webhook", "Webhook Demo", "https://webhook.site/pricewise-demo", DaysAgo(12), CreatedBy),
        new(Guid.Parse("70000000-0000-0000-0000-000000000002"), Guid.Empty, "Email", "Email Demo", "demo@pricewise.com", DaysAgo(12), CreatedBy)
    ];

    private sealed record ProductSeed(Guid Id, Guid UserId, string Name, string Description, string Brand, string Category, string ProductUrl, string ImageUrl, bool IsActive, DateTime CreatedAtUtc, string CreatedBy);
    private sealed record StoreSeed(Guid Id, Guid UserId, string Name, string BaseUrl, string LogoUrl, DateTime CreatedAtUtc, string CreatedBy);
    private sealed record PriceHistorySeed(Guid Id, Guid UserId, Guid ProductId, Guid StoreId, decimal Price, DateTime CapturedAt, string SourceUrl, DateTime CreatedAtUtc, string CreatedBy);
    private sealed record PriceAlertSeed(Guid Id, Guid UserId, Guid ProductId, decimal TargetPrice, bool IsActive, DateTime CreatedAtUtc, string CreatedBy);
    private sealed record AlertNotificationSeed(Guid Id, Guid UserId, Guid PriceAlertId, Guid ProductId, Guid PriceHistoryId, decimal TriggeredPrice, decimal TargetPrice, DateTime TriggeredAt, DateTime CreatedAtUtc, string CreatedBy);
    private sealed record NotificationChannelSeed(Guid Id, Guid UserId, string Type, string Name, string Destination, DateTime CreatedAtUtc, string CreatedBy);
}
