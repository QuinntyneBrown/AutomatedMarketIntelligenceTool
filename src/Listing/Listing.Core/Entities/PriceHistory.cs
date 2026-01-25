namespace Listing.Core.Entities;

/// <summary>
/// Represents a price history record for a listing.
/// </summary>
public sealed class PriceHistory
{
    public Guid Id { get; init; }
    public Guid ListingId { get; init; }
    public decimal Price { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
}
