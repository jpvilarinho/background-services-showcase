namespace BackgroundServicesShowcase.Api.Configuration;

public sealed class ExternalApiConfig
{
    public const string SectionName = "ExternalApi";

    public string BaseUrl { get; set; } = "https://httpstat.us";

    public int TimeoutSeconds { get; set; } = 5;

    public int RetryCount { get; set; } = 3;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
