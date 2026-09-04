using System.Runtime.CompilerServices;
using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Interfaces;

namespace BackgroundServicesShowcase.Api.Tests.TestDoubles;

public sealed class FakeEmailQueueService(IEnumerable<EmailMessage> messages) : IEmailQueueService
{
    private readonly Queue<EmailMessage> _queue = new(messages);

    public int ApproximateDepth => _queue.Count;

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _queue.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<EmailMessage> DequeueAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (_queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return _queue.Dequeue();
            await Task.Yield();
        }
    }
}
