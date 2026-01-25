using Shared.Contracts.Events;

namespace Listing.Core.Events;

/// <summary>
/// Event raised when a listing's price changes.
/// </summary>
public sealed record PriceChangedEvent : IntegrationEvent
{
    public Guid ListingId { get; init; }
    public string Source { get; init; } = string.Empty;
    public string SourceListingId { get; init; } = string.Empty;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceChange { get; init; }
    public double PercentageChange { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
}
