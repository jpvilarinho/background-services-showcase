using BackgroundServicesShowcase.Worker;
using BackgroundServicesShowcase.Worker.Configuration;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configuration => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day));

builder.Services
    .AddOptions<FeedPollerConfig>()
    .Bind(builder.Configuration.GetSection(FeedPollerConfig.SectionName))
    .ValidateOnStart();

var pollerOptions = builder.Configuration.GetSection(FeedPollerConfig.SectionName).Get<FeedPollerConfig>()
    ?? new FeedPollerConfig();

builder.Services.AddHttpClient(nameof(FeedPollerWorker), client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: pollerOptions.RetryCount,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: pollerOptions.CircuitBreakerFailureThreshold,
            durationOfBreak: TimeSpan.FromSeconds(pollerOptions.CircuitBreakerDurationSeconds)));

builder.Services.AddHostedService<FeedPollerWorker>();

var host = builder.Build();
host.Run();
