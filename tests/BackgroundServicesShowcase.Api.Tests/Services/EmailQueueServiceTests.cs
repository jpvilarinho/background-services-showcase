using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Services;
using Xunit;

namespace BackgroundServicesShowcase.Api.Tests.Services;

public sealed class EmailQueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_IncreasesApproximateDepth()
    {
        var queue = new EmailQueueService();
        var message = EmailMessage.Create("a@test.com", "subject", "body");

        await queue.EnqueueAsync(message, CancellationToken.None);

        Assert.Equal(1, queue.ApproximateDepth);
    }

    [Fact]
    public async Task DequeueAllAsync_ReturnsMessagesInFifoOrder_AndDecreasesDepth()
    {
        var queue = new EmailQueueService();
        var first = EmailMessage.Create("a@test.com", "s1", "b1");
        var second = EmailMessage.Create("b@test.com", "s2", "b2");

        await queue.EnqueueAsync(first, CancellationToken.None);
        await queue.EnqueueAsync(second, CancellationToken.None);

        var received = new List<EmailMessage>();

        using var cts = new CancellationTokenSource();

        try
        {
            await foreach (var message in queue.DequeueAllAsync(cts.Token))
            {
                received.Add(message);

                if (received.Count == 2)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        Assert.Equal(new[] { first, second }, received);
        Assert.Equal(0, queue.ApproximateDepth);
    }
}
