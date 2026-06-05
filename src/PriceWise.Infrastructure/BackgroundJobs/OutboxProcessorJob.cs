using Microsoft.Extensions.Logging;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Outbox;
using Quartz;

namespace PriceWise.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class OutboxProcessorJob : IJob
{
    private readonly IOutboxService outboxService;
    private readonly ILogger<OutboxProcessorJob> logger;
    private readonly IApplicationTelemetry telemetry;

    public OutboxProcessorJob(
        IOutboxService outboxService,
        ILogger<OutboxProcessorJob> logger,
        IApplicationTelemetry telemetry)
    {
        this.outboxService = outboxService;
        this.logger = logger;
        this.telemetry = telemetry;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = telemetry.StartActivity("OutboxProcessorJob.Execute");
        logger.LogInformation("Iniciando processamento da outbox.");

        var processed = await outboxService.ProcessPendingAsync(context.CancellationToken);

        logger.LogInformation(
            "Processamento da outbox concluído. Mensagens avaliadas: {ProcessedMessages}",
            processed);
    }
}
