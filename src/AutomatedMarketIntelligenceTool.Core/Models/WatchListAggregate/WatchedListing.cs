using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;

public class WatchedListing
{
    public WatchedListingId WatchedListingId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public ListingId ListingId { get; private set; } = null!;
    public Listing Listing { get; private set; } = null!;
    public string? Notes { get; private set; }
    public bool NotifyOnPriceChange { get; private set; }
    public bool NotifyOnRemoval { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WatchedListing() { }

    public static WatchedListing Create(
        Guid tenantId,
        ListingId listingId,
        string? notes = null,
        bool notifyOnPriceChange = true,
        bool notifyOnRemoval = true)
    {
        return new WatchedListing
        {
            WatchedListingId = WatchedListingId.CreateNew(),
            TenantId = tenantId,
            ListingId = listingId,
            Notes = notes,
            NotifyOnPriceChange = notifyOnPriceChange,
            NotifyOnRemoval = notifyOnRemoval,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    public void SetNotifications(bool onPriceChange, bool onRemoval)
    {
        NotifyOnPriceChange = onPriceChange;
        NotifyOnRemoval = onRemoval;
    }
}
