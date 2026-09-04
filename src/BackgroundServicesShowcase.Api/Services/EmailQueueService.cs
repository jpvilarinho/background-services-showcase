using System.Threading.Channels;
using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Interfaces;

namespace BackgroundServicesShowcase.Api.Services;

public sealed class EmailQueueService : IEmailQueueService
{
    private readonly Channel<EmailMessage> _channel;
    private int _approximateDepth;

    public EmailQueueService()
    {
        var options = new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<EmailMessage>(options);
    }

    public int ApproximateDepth => Volatile.Read(ref _approximateDepth);

    public async ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _channel.Writer.WriteAsync(message, cancellationToken);
        Interlocked.Increment(ref _approximateDepth);
    }

    public async IAsyncEnumerable<EmailMessage> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            Interlocked.Decrement(ref _approximateDepth);
            yield return message;
        }
    }
}
