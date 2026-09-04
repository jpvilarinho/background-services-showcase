using BackgroundServicesShowcase.Api.Contracts;
using BackgroundServicesShowcase.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BackgroundServicesShowcase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EmailController(IEmailQueueService queue) : ControllerBase
{
    private readonly IEmailQueueService _queue = queue;

    public sealed record EnqueueEmailRequest(string To, string Subject, string Body);

    [HttpPost]
    public async Task<IActionResult> Enqueue([FromBody] EnqueueEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.To))
        {
            return BadRequest("Field 'To' is required.");
        }

        var message = EmailMessage.Create(request.To, request.Subject, request.Body);
        await _queue.EnqueueAsync(message, cancellationToken);

        return Accepted(new { message.Id, QueueDepth = _queue.ApproximateDepth });
    }

    [HttpGet("queue-depth")]
    public IActionResult QueueDepth() => Ok(new { Depth = _queue.ApproximateDepth });
}
