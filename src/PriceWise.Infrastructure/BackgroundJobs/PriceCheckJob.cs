using Microsoft.Extensions.Logging;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.PriceChecks;
using Quartz;

namespace PriceWise.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class PriceCheckJob : IJob
{
    private readonly IPriceCheckService priceCheckService;
    private readonly ILogger<PriceCheckJob> logger;
    private readonly IApplicationTelemetry telemetry;

    public PriceCheckJob(
        IPriceCheckService priceCheckService,
        ILogger<PriceCheckJob> logger,
        IApplicationTelemetry telemetry)
    {
        this.priceCheckService = priceCheckService;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = telemetry.StartActivity("PriceCheckJob.Execute");
        logger.LogInformation("Starting scheduled price check job");

        var result = await priceCheckService.RunAsync(PriceCheckTrigger.Automatic, context.CancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Scheduled price check job finished with status {Status}",
                result.Value.Status);

            return;
        }

        logger.LogWarning(
            "Scheduled price check job finished with error {ErrorCode}",
            result.Error.Code);
    }
}
