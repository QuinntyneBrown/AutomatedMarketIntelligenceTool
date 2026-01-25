using Deduplication.Core.Enums;
using Shared.Contracts.Events;

namespace Deduplication.Core.Events;

/// <summary>
/// Event raised when a duplicate is detected.
/// </summary>
public sealed record DuplicateFoundEvent : IntegrationEvent
{
    public Guid DuplicateMatchId { get; init; }
    public Guid SourceListingId { get; init; }
    public Guid TargetListingId { get; init; }
    public double MatchScore { get; init; }
    public MatchConfidence Confidence { get; init; }
}
