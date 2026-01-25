namespace WatchList.Core.Entities;

/// <summary>
/// Represents a listing that a user is watching for price changes.
/// </summary>
public sealed class WatchedListing
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ListingId { get; init; }
    public string? Notes { get; private set; }
    public decimal PriceAtWatch { get; init; }
    public decimal CurrentPrice { get; private set; }
    public decimal PriceChange { get; private set; }
    public DateTimeOffset AddedAt { get; init; }
    public DateTimeOffset LastCheckedAt { get; private set; }

    private WatchedListing() { }

    public static WatchedListing Create(
        Guid userId,
        Guid listingId,
        decimal currentPrice,
        string? notes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new WatchedListing
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ListingId = listingId,
            PriceAtWatch = currentPrice,
            CurrentPrice = currentPrice,
            PriceChange = 0,
            Notes = notes,
            AddedAt = now,
            LastCheckedAt = now
        };
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (CurrentPrice != newPrice)
        {
            CurrentPrice = newPrice;
            PriceChange = newPrice - PriceAtWatch;
        }
        LastCheckedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}
