using BackgroundServicesShowcase.Api.Contracts;

namespace BackgroundServicesShowcase.Api.Interfaces;

public interface IEmailSenderService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
