using Shared.Contracts.Events;

namespace Deduplication.Core.Events;

/// <summary>
/// Event raised when a potential match requires manual review.
/// </summary>
public sealed record ReviewRequiredEvent : IntegrationEvent
{
    public Guid ReviewItemId { get; init; }
    public Guid SourceListingId { get; init; }
    public Guid TargetListingId { get; init; }
    public double MatchScore { get; init; }
    public int Priority { get; init; }
}
