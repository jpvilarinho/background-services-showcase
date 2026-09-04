using BackgroundServicesShowcase.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace BackgroundServicesShowcase.Worker;

public sealed class FeedPollerWorker(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<FeedPollerConfig> options,
    ILogger<FeedPollerWorker> logger) : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<FeedPollerConfig> _options = options;
    private readonly ILogger<FeedPollerWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FeedPollerWorker started at {StartTime}", DateTimeOffset.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollOnceAsync(stoppingToken);

            var interval = TimeSpan.FromSeconds(_options.CurrentValue.IntervalSeconds);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("FeedPollerWorker stopping at {StopTime}", DateTimeOffset.UtcNow);
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(FeedPollerWorker));

        try
        {
            var response = await client.GetAsync(_options.CurrentValue.FeedUrl, cancellationToken);
            _logger.LogInformation("Feed poll completed with status {StatusCode}", response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feed poll failed, will retry on next interval");
        }
    }
}
