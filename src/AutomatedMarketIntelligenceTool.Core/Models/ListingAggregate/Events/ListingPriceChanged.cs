namespace AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Events;

public class ListingPriceChanged
{
    public ListingId ListingId { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public DateTime ChangedAt { get; init; }

    public ListingPriceChanged(
        ListingId listingId,
        decimal oldPrice,
        decimal newPrice,
        DateTime changedAt)
    {
        ListingId = listingId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        ChangedAt = changedAt;
    }
}
