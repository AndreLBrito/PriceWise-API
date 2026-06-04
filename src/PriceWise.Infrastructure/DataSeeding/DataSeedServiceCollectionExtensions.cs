using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.DataSeeding;

namespace PriceWise.Infrastructure.DataSeeding;

public static class DataSeedServiceCollectionExtensions
{
    public static IServiceCollection AddDataSeed(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DataSeedOptions>(options =>
        {
            options.Enabled = ReadBool(configuration, $"{DataSeedOptions.SectionName}:Enabled", true);
            options.CreateDemoUser = ReadBool(configuration, $"{DataSeedOptions.SectionName}:CreateDemoUser", true);
            options.DemoUserEmail = configuration[$"{DataSeedOptions.SectionName}:DemoUserEmail"] ?? "demo@pricewise.com";
            options.DemoUserPassword = configuration[$"{DataSeedOptions.SectionName}:DemoUserPassword"] ?? "Demo@123456";
            options.CreateDemoData = ReadBool(configuration, $"{DataSeedOptions.SectionName}:CreateDemoData", true);
        });
        services.AddScoped<IDataSeeder, DemoDataSeeder>();

        return services;
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        return bool.TryParse(configuration[key], out var value) ? value : defaultValue;
    }
}
