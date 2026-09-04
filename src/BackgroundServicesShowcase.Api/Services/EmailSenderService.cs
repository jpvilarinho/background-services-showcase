using BackgroundServicesShowcase.Api.Configuration;
using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace BackgroundServicesShowcase.Api.Services;

public sealed class EmailSenderService(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<EmailWorkerConfig> options,
    ILogger<EmailSenderService> logger) : IEmailSenderService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.ExternalApi);
    private readonly IOptionsMonitor<EmailWorkerConfig> _options = options;
    private readonly ILogger<EmailSenderService> _logger = logger;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var sender = _options.CurrentValue.SenderAddress;

        _logger.LogInformation(
            "Sending email {EmailId} from {Sender} to {Recipient} (subject: {Subject})",
            message.Id, sender, message.To, message.Subject);

        var response = await _httpClient.GetAsync("/200", cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Email {EmailId} dispatched successfully", message.Id);
    }
}

public static class HttpClientNames
{
    public const string ExternalApi = "ExternalApi";
}
