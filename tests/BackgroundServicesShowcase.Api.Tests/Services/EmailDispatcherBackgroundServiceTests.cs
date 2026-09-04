using BackgroundServicesShowcase.Api.Configuration;
using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Interfaces;
using BackgroundServicesShowcase.Api.Services;
using BackgroundServicesShowcase.Api.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BackgroundServicesShowcase.Api.Tests.Services;

public sealed class EmailDispatcherBackgroundServiceTests
{
    private static IOptionsMonitor<EmailWorkerConfig> CreateOptions(EmailWorkerConfig config)
    {
        var mock = new Mock<IOptionsMonitor<EmailWorkerConfig>>();
        mock.SetupGet(m => m.CurrentValue).Returns(config);
        return mock.Object;
    }

    private static IServiceScopeFactory CreateScopeFactory(IEmailSenderService sender)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => sender);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_DispatchesAllQueuedMessages()
    {
        var messages = new[]
        {
            EmailMessage.Create("a@test.com", "s1", "b1"),
            EmailMessage.Create("b@test.com", "s2", "b2"),
            EmailMessage.Create("c@test.com", "s3", "b3")
        };

        var senderMock = new Mock<IEmailSenderService>();
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var queue = new FakeEmailQueueService(messages);
        var options = CreateOptions(new EmailWorkerConfig { MaxBatchSize = 10, PollingIntervalSeconds = 0 });
        var scopeFactory = CreateScopeFactory(senderMock.Object);

        var service = new EmailDispatcherBackgroundService(
            queue, scopeFactory, options, NullLogger<EmailDispatcherBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        senderMock.Verify(
            s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(messages.Length));

        foreach (var message in messages)
        {
            senderMock.Verify(s => s.SendAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesProcessingWhenOneMessageFails()
    {
        var messages = new[]
        {
            EmailMessage.Create("a@test.com", "s1", "b1"),
            EmailMessage.Create("fails@test.com", "s2", "b2"),
            EmailMessage.Create("c@test.com", "s3", "b3")
        };

        var senderMock = new Mock<IEmailSenderService>();
        senderMock
            .Setup(s => s.SendAsync(It.Is<EmailMessage>(m => m.To == "fails@test.com"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated failure"));
        senderMock
            .Setup(s => s.SendAsync(It.Is<EmailMessage>(m => m.To != "fails@test.com"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var queue = new FakeEmailQueueService(messages);
        var options = CreateOptions(new EmailWorkerConfig { MaxBatchSize = 10, PollingIntervalSeconds = 0 });
        var scopeFactory = CreateScopeFactory(senderMock.Object);

        var service = new EmailDispatcherBackgroundService(
            queue, scopeFactory, options, NullLogger<EmailDispatcherBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask!.IsCompletedSuccessfully);

        senderMock.Verify(
            s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(messages.Length));
    }
}
