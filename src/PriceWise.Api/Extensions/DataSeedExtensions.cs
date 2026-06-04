using PriceWise.Application.Abstractions.DataSeeding;

namespace PriceWise.Api.Extensions;

public static class DataSeedExtensions
{
    public static async Task<WebApplication> UseDemoDataSeedAsync(this WebApplication app)
    {
        var environment = app.Services.GetRequiredService<IHostEnvironment>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeed");

        if (environment.IsProduction())
        {
            logger.LogInformation("Seed de demonstração não será executado em Production.");
            return app;
        }

        using var scope = app.Services.CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        var result = await dataSeeder.SeedAsync();

        if (result.IsFailure)
        {
            logger.LogWarning("Seed de demonstração não foi executado: {Message}", result.Error.Message);
        }

        return app;
    }
}
