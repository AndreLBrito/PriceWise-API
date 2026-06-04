using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PriceWise.Application.Abstractions.Auth;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Infrastructure.Authentication;
using PriceWise.Infrastructure.Database;
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
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
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
}
