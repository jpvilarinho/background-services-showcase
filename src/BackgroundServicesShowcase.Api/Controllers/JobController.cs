using BackgroundServicesShowcase.Api.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace BackgroundServicesShowcase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobController(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager) : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager = recurringJobManager;

    [HttpPost("report-cleanup/run-once")]
    public IActionResult RunOnce([FromQuery] int retentionDays = 30)
    {
        var jobId = _backgroundJobClient.Enqueue<ReportCleanupJob>(job => job.RunAsync(retentionDays, CancellationToken.None));
        return Accepted(new { JobId = jobId });
    }

    [HttpPost("report-cleanup/schedule-recurring")]
    public IActionResult ScheduleRecurring([FromQuery] int retentionDays = 30)
    {
        _recurringJobManager.AddOrUpdate<ReportCleanupJob>(
            recurringJobId: "report-cleanup-daily",
            methodCall: job => job.RunAsync(retentionDays, CancellationToken.None),
            cronExpression: Cron.Daily);

        return Ok(new { RecurringJobId = "report-cleanup-daily", Cron = Cron.Daily() });
    }
}
