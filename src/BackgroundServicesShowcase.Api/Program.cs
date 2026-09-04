using BackgroundServicesShowcase.Api.Configuration;
using BackgroundServicesShowcase.Api.Extensions;
using BackgroundServicesShowcase.Api.Security;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddShowcaseOptions(builder.Configuration);
    builder.Services.AddResilientExternalApiClient(builder.Configuration);
    builder.Services.AddShowcaseEmailPipeline();
    builder.Services.AddShowcaseQuartz();
    builder.Services.AddShowcaseHangfire();
    builder.Services.AddShowcaseObservability();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.MapControllers();

    var hangfireDashboardConfig = app.Configuration.GetSection(HangfireDashboardConfig.SectionName).Get<HangfireDashboardConfig>()
        ?? new HangfireDashboardConfig();

    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter(hangfireDashboardConfig) },
        IgnoreAntiforgeryToken = true
    });

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var payload = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                })
            };
            await context.Response.WriteAsJsonAsync(payload);
        }
    });

    Log.Information("BackgroundServicesShowcase.Api starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BackgroundServicesShowcase.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
