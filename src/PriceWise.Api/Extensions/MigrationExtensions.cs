using FluentMigrator.Runner;

namespace PriceWise.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task<WebApplication> UseDatabaseMigrationsAsync(this WebApplication app)
    {
        const int maxAttempts = 5;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigrations");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();

                logger.LogInformation("Migrations executadas com sucesso.");
                return app;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Falha ao executar migrations. Tentativa {Attempt} de {MaxAttempts}.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }

        using var finalScope = app.Services.CreateScope();
        var finalRunner = finalScope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        finalRunner.MigrateUp();

        logger.LogInformation("Migrations executadas com sucesso.");
        return app;
    }
}
