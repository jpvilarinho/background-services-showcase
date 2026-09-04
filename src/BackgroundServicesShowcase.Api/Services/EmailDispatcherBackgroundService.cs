using BackgroundServicesShowcase.Api.Configuration;
using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace BackgroundServicesShowcase.Api.Services;

public sealed class EmailDispatcherBackgroundService(
    IEmailQueueService queue,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<EmailWorkerConfig> options,
    ILogger<EmailDispatcherBackgroundService> logger) : BackgroundService
{
    private readonly IEmailQueueService _queue = queue;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IOptionsMonitor<EmailWorkerConfig> _options = options;
    private readonly ILogger<EmailDispatcherBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailDispatcherBackgroundService starting");

        var buffer = new List<EmailMessage>();

        await foreach (var message in _queue.DequeueAllAsync(stoppingToken))
        {
            buffer.Add(message);

            var maxBatchSize = _options.CurrentValue.MaxBatchSize;
            var queueIsQuiet = _queue.ApproximateDepth == 0;

            if (buffer.Count < maxBatchSize && !queueIsQuiet)
            {
                continue;
            }

            await DispatchBatchAsync(buffer, stoppingToken);
            buffer.Clear();

            var delaySeconds = _options.CurrentValue.PollingIntervalSeconds;
            if (delaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }

        _logger.LogInformation("EmailDispatcherBackgroundService stopping");
    }

    private async Task DispatchBatchAsync(List<EmailMessage> batch, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();

        _logger.LogInformation("Dispatching batch of {Count} email(s)", batch.Count);

        foreach (var message in batch)
        {
            try
            {
                await sender.SendAsync(message, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch email {EmailId} to {Recipient}", message.Id, message.To);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EmailDispatcherBackgroundService received stop signal, finishing gracefully");
        await base.StopAsync(cancellationToken);
    }
}
