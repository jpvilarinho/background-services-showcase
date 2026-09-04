using Quartz;

namespace BackgroundServicesShowcase.Api.Jobs;

[DisallowConcurrentExecution]
public sealed class InventorySyncJob(ILogger<InventorySyncJob> logger) : IJob
{
    private readonly ILogger<InventorySyncJob> _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation(
            "InventorySyncJob fired at {FireTime}, next fire at {NextFire}",
            context.FireTimeUtc, context.NextFireTimeUtc);

        await Task.Delay(TimeSpan.FromMilliseconds(250), context.CancellationToken);

        _logger.LogInformation("InventorySyncJob completed");
    }
}
