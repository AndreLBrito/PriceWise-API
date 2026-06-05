using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Outbox;
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
        services.Configure<OutboxOptions>(outboxOptions =>
        {
            var readOptions = ReadOutboxOptions(configuration);

            outboxOptions.Enabled = readOptions.Enabled;
            outboxOptions.IntervalInSeconds = readOptions.IntervalInSeconds;
            outboxOptions.MaxRetries = readOptions.MaxRetries;
            outboxOptions.BatchSize = readOptions.BatchSize;
        });

        services.AddQuartz(quartz =>
        {
            var priceCheckJobKey = new JobKey(nameof(PriceCheckJob));
            var outboxOptions = ReadOutboxOptions(configuration);
            var outboxJobKey = new JobKey(nameof(OutboxProcessorJob));

            quartz.AddJob<PriceCheckJob>(job => job.WithIdentity(priceCheckJobKey));
            quartz.AddJob<OutboxProcessorJob>(job => job.WithIdentity(outboxJobKey));

            if (options.Enabled)
            {
                quartz.AddTrigger(trigger => trigger
                    .ForJob(priceCheckJobKey)
                    .WithIdentity($"{nameof(PriceCheckJob)}Trigger")
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule
                        .WithIntervalInMinutes(Math.Max(1, options.IntervalInMinutes))
                        .RepeatForever()));
            }

            if (outboxOptions.Enabled)
            {
                quartz.AddTrigger(trigger => trigger
                    .ForJob(outboxJobKey)
                    .WithIdentity($"{nameof(OutboxProcessorJob)}Trigger")
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule
                        .WithIntervalInSeconds(Math.Max(5, outboxOptions.IntervalInSeconds))
                        .RepeatForever()));
            }
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

    private static OutboxOptions ReadOutboxOptions(IConfiguration configuration)
    {
        return new OutboxOptions
        {
            Enabled = ReadBool(configuration, $"{OutboxOptions.SectionName}:Enabled", true),
            IntervalInSeconds = ReadInt(configuration, $"{OutboxOptions.SectionName}:IntervalInSeconds", 30),
            MaxRetries = ReadInt(configuration, $"{OutboxOptions.SectionName}:MaxRetries", 5),
            BatchSize = ReadInt(configuration, $"{OutboxOptions.SectionName}:BatchSize", 20)
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
