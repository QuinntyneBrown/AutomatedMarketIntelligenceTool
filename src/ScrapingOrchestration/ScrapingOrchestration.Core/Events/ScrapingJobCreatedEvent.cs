using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;
using Shared.Contracts.Events;

namespace ScrapingOrchestration.Core.Events;

/// <summary>
/// Event raised when a scraping job is created.
/// </summary>
public sealed record ScrapingJobCreatedEvent : IntegrationEvent
{
    public Guid JobId { get; init; }
    public Guid SessionId { get; init; }
    public ScrapingSource Source { get; init; }
    public SearchParameters Parameters { get; init; } = new();
}
