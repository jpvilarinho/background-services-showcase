namespace BackgroundServicesShowcase.Api.Contracts;

public sealed record EmailMessage(
    Guid Id,
    string To,
    string Subject,
    string Body,
    DateTimeOffset EnqueuedAtUtc)
{
    public static EmailMessage Create(string to, string subject, string body) =>
        new(Guid.NewGuid(), to, subject, body, DateTimeOffset.UtcNow);
}
