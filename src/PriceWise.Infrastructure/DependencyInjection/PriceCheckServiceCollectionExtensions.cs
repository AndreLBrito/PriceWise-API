using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.PriceChecks;
using PriceWise.Infrastructure.BackgroundJobs;
using PriceWise.Infrastructure.Repositories;
using Quartz;

namespace PriceWise.Infrastructure.DependencyInjection;

public static class PriceCheckServiceCollectionExtensions
{
    public static IServiceCollection AddPriceCheckBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = ReadOptions(configuration);

        services.Configure<PriceCheckOptions>(priceCheckOptions =>
        {
            priceCheckOptions.Enabled = options.Enabled;
            priceCheckOptions.IntervalInMinutes = options.IntervalInMinutes;
            priceCheckOptions.MaxProductsPerExecution = options.MaxProductsPerExecution;
        });
        services.AddScoped<IPriceCheckRepository, PriceCheckRepository>();

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(PriceCheckJob));

            quartz.AddJob<PriceCheckJob>(job => job.WithIdentity(jobKey));

            if (!options.Enabled)
            {
                return;
            }

            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(PriceCheckJob)}Trigger")
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInMinutes(Math.Max(1, options.IntervalInMinutes))
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(hostedService =>
        {
            hostedService.WaitForJobsToComplete = true;
        });

        return services;
    }

    private static PriceCheckOptions ReadOptions(IConfiguration configuration)
    {
        return new PriceCheckOptions
        {
            Enabled = ReadBool(configuration, $"{PriceCheckOptions.SectionName}:Enabled", true),
            IntervalInMinutes = ReadInt(configuration, $"{PriceCheckOptions.SectionName}:IntervalInMinutes", 30),
            MaxProductsPerExecution = ReadInt(configuration, $"{PriceCheckOptions.SectionName}:MaxProductsPerExecution", 50)
        };
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        return bool.TryParse(configuration[key], out var value) ? value : defaultValue;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        return int.TryParse(configuration[key], out var value) ? value : defaultValue;
    }
}
