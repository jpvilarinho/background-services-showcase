using BackgroundServicesShowcase.Api.Interfaces;
using BackgroundServicesShowcase.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace BackgroundServicesShowcase.Api.Tests.Services;

public sealed class EmailQueueHealthCheckServiceTests
{
    [Theory]
    [InlineData(0, HealthStatus.Healthy)]
    [InlineData(99, HealthStatus.Healthy)]
    [InlineData(100, HealthStatus.Degraded)]
    [InlineData(500, HealthStatus.Degraded)]
    public async Task CheckHealthAsync_ReturnsExpectedStatus_BasedOnQueueDepth(int depth, HealthStatus expected)
    {
        var queueMock = new Mock<IEmailQueueService>();
        queueMock.SetupGet(q => q.ApproximateDepth).Returns(depth);

        var healthCheck = new EmailQueueHealthCheckService(queueMock.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(expected, result.Status);
    }
}
