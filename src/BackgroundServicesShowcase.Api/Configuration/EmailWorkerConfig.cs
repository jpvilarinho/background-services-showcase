namespace BackgroundServicesShowcase.Api.Configuration;

public sealed class EmailWorkerConfig
{
    public const string SectionName = "EmailWorker";

    public int PollingIntervalSeconds { get; set; } = 5;

    public int MaxBatchSize { get; set; } = 10;

    public string SenderAddress { get; set; } = "no-reply@showcase.local";
}
