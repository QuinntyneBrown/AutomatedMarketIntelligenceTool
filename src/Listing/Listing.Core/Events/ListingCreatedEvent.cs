using Shared.Contracts.Events;

namespace Listing.Core.Events;

/// <summary>
/// Event raised when a listing is created.
/// </summary>
public sealed record ListingCreatedEvent : IntegrationEvent
{
    public Guid ListingId { get; init; }
    public string SourceListingId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public string? VIN { get; init; }
    public string? PrimaryImageUrl { get; init; }
}
