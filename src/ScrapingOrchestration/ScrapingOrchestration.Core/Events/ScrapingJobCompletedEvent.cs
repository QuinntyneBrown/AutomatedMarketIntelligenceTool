using ScrapingOrchestration.Core.Enums;
using Shared.Contracts.Events;

namespace ScrapingOrchestration.Core.Events;

/// <summary>
/// Event raised when a scraping job completes successfully.
/// </summary>
public sealed record ScrapingJobCompletedEvent : IntegrationEvent
{
    public Guid JobId { get; init; }
    public Guid SessionId { get; init; }
    public ScrapingSource Source { get; init; }
    public int ListingsFound { get; init; }
    public int PagesCrawled { get; init; }
    public TimeSpan Duration { get; init; }
}
