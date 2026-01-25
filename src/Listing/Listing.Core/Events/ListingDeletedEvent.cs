using Shared.Contracts.Events;

namespace Listing.Core.Events;

/// <summary>
/// Event raised when a listing is deleted or deactivated.
/// </summary>
public sealed record ListingDeletedEvent : IntegrationEvent
{
    public Guid ListingId { get; init; }
    public string Source { get; init; } = string.Empty;
    public string SourceListingId { get; init; } = string.Empty;
}
