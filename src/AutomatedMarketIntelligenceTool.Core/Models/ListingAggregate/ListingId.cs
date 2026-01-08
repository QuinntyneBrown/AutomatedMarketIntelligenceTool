namespace AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

public record ListingId(Guid Value)
{
    public static ListingId Create() => new(Guid.NewGuid());

    public static implicit operator Guid(ListingId listingId) => listingId.Value;

    public static implicit operator ListingId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
