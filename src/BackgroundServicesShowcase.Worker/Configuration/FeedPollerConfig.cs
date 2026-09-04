namespace BackgroundServicesShowcase.Worker.Configuration;

public sealed class FeedPollerConfig
{
    public const string SectionName = "FeedPoller";

    public int IntervalSeconds { get; set; } = 15;

    public string FeedUrl { get; set; } = "https://httpstat.us/200";

    public int RetryCount { get; set; } = 3;

    public int CircuitBreakerFailureThreshold { get; set; } = 4;

    public int CircuitBreakerDurationSeconds { get; set; } = 20;
}
