using Microsoft.Extensions.Logging;
using PriceWise.Application.PriceChecks;
using Quartz;

namespace PriceWise.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class PriceCheckJob : IJob
{
    private readonly IPriceCheckService priceCheckService;
    private readonly ILogger<PriceCheckJob> logger;

    public PriceCheckJob(IPriceCheckService priceCheckService, ILogger<PriceCheckJob> logger)
    {
        this.priceCheckService = priceCheckService;
        this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting scheduled price check job");

        var result = await priceCheckService.RunAsync(context.CancellationToken);

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
