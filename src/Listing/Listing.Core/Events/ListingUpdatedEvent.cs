using Shared.Contracts.Events;

namespace Listing.Core.Events;

/// <summary>
/// Event raised when a listing is updated.
/// </summary>
public sealed record ListingUpdatedEvent : IntegrationEvent
{
    public Guid ListingId { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool PriceChanged { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal? NewPrice { get; init; }
}
