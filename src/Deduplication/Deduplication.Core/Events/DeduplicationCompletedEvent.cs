using Shared.Contracts.Events;

namespace Deduplication.Core.Events;

/// <summary>
/// Event raised when deduplication analysis is completed.
/// </summary>
public sealed record DeduplicationCompletedEvent : IntegrationEvent
{
    public Guid ListingId { get; init; }
    public int DuplicatesFound { get; init; }
    public int ReviewItemsCreated { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}
