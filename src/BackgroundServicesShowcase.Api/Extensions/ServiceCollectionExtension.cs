using BackgroundServicesShowcase.Api.Configuration;
using BackgroundServicesShowcase.Api.Interfaces;
using BackgroundServicesShowcase.Api.Jobs;
using BackgroundServicesShowcase.Api.Services;
using Hangfire;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Quartz;

namespace BackgroundServicesShowcase.Api.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddShowcaseOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<EmailWorkerConfig>()
            .Bind(configuration.GetSection(EmailWorkerConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<ExternalApiConfig>()
            .Bind(configuration.GetSection(ExternalApiConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddResilientExternalApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = configuration.GetSection(ExternalApiConfig.SectionName).Get<ExternalApiConfig>()
            ?? new ExternalApiConfig();

        services.AddHttpClient(HttpClientNames.ExternalApi, client =>
            {
                client.BaseAddress = new Uri(apiOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
            })
            .AddPolicyHandler(BuildRetryPolicy(apiOptions))
            .AddPolicyHandler(BuildCircuitBreakerPolicy(apiOptions));

        services.AddScoped<IEmailSenderService, EmailSenderService>();

        return services;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy(ExternalApiConfig options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: options.RetryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    Console.WriteLine(
                        $"[Polly] Retry {attempt} after {delay.TotalSeconds}s due to {outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message}");
                });
    }

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy(ExternalApiConfig options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                onBreak: (outcome, breakDelay) =>
                    Console.WriteLine($"[Polly] Circuit opened for {breakDelay.TotalSeconds}s"),
                onReset: () => Console.WriteLine("[Polly] Circuit closed again"),
                onHalfOpen: () => Console.WriteLine("[Polly] Circuit half-open, testing next call"));
    }

    public static IServiceCollection AddShowcaseEmailPipeline(this IServiceCollection services)
    {
        services.AddSingleton<IEmailQueueService, EmailQueueService>();
        services.AddHostedService<EmailDispatcherBackgroundService>();
        return services;
    }

    public static IServiceCollection AddShowcaseQuartz(this IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(InventorySyncJob));

            quartz.AddJob<InventorySyncJob>(options => options.WithIdentity(jobKey));

            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(InventorySyncJob)}-trigger")
                .WithCronSchedule("0/30 * * * * ?"));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }

    public static IServiceCollection AddShowcaseHangfire(this IServiceCollection services)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage());

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount;
            options.ServerName = "background-services-showcase";
        });

        services.AddScoped<ReportCleanupJob>();

        return services;
    }

    public static IServiceCollection AddShowcaseObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("BackgroundServicesShowcase.Api"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Quartz")
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter());

        services.AddHealthChecks()
            .AddCheck<EmailQueueHealthCheckService>("email-queue");

        return services;
    }
}
