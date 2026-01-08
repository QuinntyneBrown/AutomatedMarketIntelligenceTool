namespace AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Events;

public class ListingCreated
{
    public ListingId ListingId { get; init; }
    public string Make { get; init; }
    public string Model { get; init; }
    public int Year { get; init; }
    public decimal Price { get; init; }
    public string SourceSite { get; init; }
    public DateTime CreatedAt { get; init; }

    public ListingCreated(
        ListingId listingId,
        string make,
        string model,
        int year,
        decimal price,
        string sourceSite,
        DateTime createdAt)
    {
        ListingId = listingId;
        Make = make;
        Model = model;
        Year = year;
        Price = price;
        SourceSite = sourceSite;
        CreatedAt = createdAt;
    }
}
