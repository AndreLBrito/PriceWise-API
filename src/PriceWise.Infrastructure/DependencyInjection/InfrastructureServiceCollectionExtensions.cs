using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PriceWise.Application.Abstractions.Auth;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Authentication;
using PriceWise.Application.Exports;
using PriceWise.Application.PriceChecks;
using PriceWise.Infrastructure.Authentication;
using PriceWise.Infrastructure.Database;
using PriceWise.Infrastructure.DataSeeding;
using PriceWise.Infrastructure.Notifications;
using PriceWise.Infrastructure.Repositories;

namespace PriceWise.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseOptions = new DatabaseOptions
        {
            ConnectionString = configuration[$"{DatabaseOptions.SectionName}:ConnectionString"] ?? string.Empty
        };
        var jwtOptions = new JwtOptions
        {
            Issuer = configuration[$"{JwtOptions.SectionName}:Issuer"] ?? "PriceWise",
            Audience = configuration[$"{JwtOptions.SectionName}:Audience"] ?? "PriceWise",
            Secret = configuration[$"{JwtOptions.SectionName}:Secret"] ?? "pricewise-development-secret-key",
            AccessTokenExpirationMinutes = ReadInt(configuration, $"{JwtOptions.SectionName}:AccessTokenExpirationMinutes", 60),
            RefreshTokenExpirationDays = ReadInt(configuration, $"{JwtOptions.SectionName}:RefreshTokenExpirationDays", 7)
        };

        services.AddSingleton(databaseOptions);
        services.AddSingleton(jwtOptions);
        services.AddSingleton(_ => NpgsqlDataSource.Create(databaseOptions.ConnectionString));
        services.AddCacheInfrastructure(configuration);
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.Configure<AuthenticationSecurityOptions>(options =>
        {
            options.MaxFailedLoginAttempts = ReadInt(configuration, $"{AuthenticationSecurityOptions.SectionName}:MaxFailedLoginAttempts", 5);
            options.LockoutMinutes = ReadInt(configuration, $"{AuthenticationSecurityOptions.SectionName}:LockoutMinutes", 15);
        });
        services.AddSingleton<IAccessTokenProvider, JwtTokenProvider>();
        services.AddSingleton<IRefreshTokenProvider, RefreshTokenProvider>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
        services.AddScoped<IAlertNotificationRepository, AlertNotificationRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<INotificationChannelRepository, NotificationChannelRepository>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.Configure<CsvExportOptions>(options =>
        {
            options.MaxRows = ReadInt(configuration, $"{CsvExportOptions.SectionName}:MaxRows", 10_000);
            options.DateFormat = configuration[$"{CsvExportOptions.SectionName}:DateFormat"] ?? "yyyy-MM-dd HH:mm:ss";
        });
        services.Configure<PriceProviderOptions>(options =>
        {
            options.MinimumBasePrice = ReadDecimal(configuration, $"{PriceProviderOptions.SectionName}:MinimumBasePrice", 50m);
            options.MaximumBasePrice = ReadDecimal(configuration, $"{PriceProviderOptions.SectionName}:MaximumBasePrice", 1500m);
            options.VariationPercentage = ReadDecimal(configuration, $"{PriceProviderOptions.SectionName}:VariationPercentage", 0.03m);
        });
        services.Configure<WebhookNotificationOptions>(options =>
        {
            options.Enabled = ReadBool(configuration, $"{WebhookNotificationOptions.SectionName}:Enabled", true);
            options.TimeoutInSeconds = ReadInt(configuration, $"{WebhookNotificationOptions.SectionName}:TimeoutInSeconds", 10);
            options.MaxRetryAttempts = ReadInt(configuration, $"{WebhookNotificationOptions.SectionName}:MaxRetryAttempts", 3);
        });
        services.AddHttpClient<IWebhookNotificationSender, WebhookNotificationSender>();
        services.Configure<EmailNotificationOptions>(options =>
        {
            options.Enabled = ReadBool(configuration, $"{EmailNotificationOptions.SectionName}:Enabled", false);
            options.Host = configuration[$"{EmailNotificationOptions.SectionName}:Host"] ?? "localhost";
            options.Port = ReadInt(configuration, $"{EmailNotificationOptions.SectionName}:Port", 1025);
            options.UseSsl = ReadBool(configuration, $"{EmailNotificationOptions.SectionName}:UseSsl", false);
            options.UserName = configuration[$"{EmailNotificationOptions.SectionName}:UserName"];
            options.Password = configuration[$"{EmailNotificationOptions.SectionName}:Password"];
            options.FromName = configuration[$"{EmailNotificationOptions.SectionName}:FromName"] ?? "PriceWise";
            options.FromEmail = configuration[$"{EmailNotificationOptions.SectionName}:FromEmail"] ?? "noreply@pricewise.local";
            options.TimeoutInSeconds = ReadInt(configuration, $"{EmailNotificationOptions.SectionName}:TimeoutInSeconds", 10);
            options.MaxRetryAttempts = ReadInt(configuration, $"{EmailNotificationOptions.SectionName}:MaxRetryAttempts", 3);
        });
        services.AddScoped<ISmtpEmailClient, MailKitSmtpEmailClient>();
        services.AddScoped<IEmailNotificationSender, EmailNotificationSender>();
        services.AddPriceCheckBackgroundJobs(configuration);
        services.AddDataSeed(configuration);

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(databaseOptions.ConnectionString)
                .ScanIn(typeof(InfrastructureServiceCollectionExtensions).Assembly)
                .For.Migrations())
            .AddLogging(logging => logging.AddFluentMigratorConsole());

        return services;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        return int.TryParse(configuration[key], out var value) ? value : defaultValue;
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        return bool.TryParse(configuration[key], out var value) ? value : defaultValue;
    }

    private static decimal ReadDecimal(IConfiguration configuration, string key, decimal defaultValue)
    {
        return decimal.TryParse(configuration[key], out var value) ? value : defaultValue;
    }
}
