using BackgroundServicesShowcase.Api.Contracts;

namespace BackgroundServicesShowcase.Api.Interfaces;

public interface IEmailQueueService
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken);

    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken);

    int ApproximateDepth { get; }
}
