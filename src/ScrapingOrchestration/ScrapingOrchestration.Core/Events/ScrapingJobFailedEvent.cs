using ScrapingOrchestration.Core.Enums;
using Shared.Contracts.Events;

namespace ScrapingOrchestration.Core.Events;

/// <summary>
/// Event raised when a scraping job fails.
/// </summary>
public sealed record ScrapingJobFailedEvent : IntegrationEvent
{
    public Guid JobId { get; init; }
    public Guid SessionId { get; init; }
    public ScrapingSource Source { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public bool WillRetry { get; init; }
}
