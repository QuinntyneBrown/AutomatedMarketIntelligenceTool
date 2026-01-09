namespace AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;

public record PriceHistoryId(Guid Value)
{
    public static PriceHistoryId Create() => new(Guid.NewGuid());
    public static PriceHistoryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
