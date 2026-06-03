using System.Data;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PriceWise.Infrastructure.Database;

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

        services.AddSingleton(_ => NpgsqlDataSource.Create(databaseOptions.ConnectionString));
        services.AddScoped<IDbConnection>(provider => provider
            .GetRequiredService<NpgsqlDataSource>()
            .OpenConnection());

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(databaseOptions.ConnectionString)
                .ScanIn(typeof(InfrastructureServiceCollectionExtensions).Assembly)
                .For.Migrations())
            .AddLogging(logging => logging.AddFluentMigratorConsole());

        return services;
    }
}
