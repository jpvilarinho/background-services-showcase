using Hangfire;

namespace BackgroundServicesShowcase.Api.Jobs;

public sealed class ReportCleanupJob(ILogger<ReportCleanupJob> logger)
{
    private readonly ILogger<ReportCleanupJob> _logger = logger;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 15, 30 })]
    public async Task RunAsync(int retentionDays, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ReportCleanupJob started, retention window: {RetentionDays} day(s)", retentionDays);

        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);

        _logger.LogInformation("ReportCleanupJob finished, expired reports removed");
    }
}
