using Microsoft.Extensions.Logging;

namespace Shared.Messaging.Retry;

/// <summary>
/// Handles messages that have exceeded retry attempts.
/// </summary>
public class DeadLetterHandler
{
    private readonly ILogger<DeadLetterHandler> _logger;

    public DeadLetterHandler(ILogger<DeadLetterHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(string eventName, byte[] body, Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception,
            "Message for event {EventName} moved to dead letter queue after exhausting retries",
            eventName);

        // In production, this would persist to a dead letter queue/table for manual review
        return Task.CompletedTask;
    }
}
