namespace AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;

public record WatchedListingId(Guid Value)
{
    public static WatchedListingId CreateNew() => new(Guid.NewGuid());
}
