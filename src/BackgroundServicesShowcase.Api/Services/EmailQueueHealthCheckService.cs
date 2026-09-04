using BackgroundServicesShowcase.Api.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackgroundServicesShowcase.Api.Services;

public sealed class EmailQueueHealthCheckService(IEmailQueueService queue) : IHealthCheck
{
    private const int DegradedThreshold = 100;

    private readonly IEmailQueueService _queue = queue;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var depth = _queue.ApproximateDepth;

        var result = depth switch
        {
            var d when d < DegradedThreshold => HealthCheckResult.Healthy($"Queue depth: {d}"),
            _ => HealthCheckResult.Degraded($"Queue depth is high: {depth}")
        };

        return Task.FromResult(result);
    }
}
