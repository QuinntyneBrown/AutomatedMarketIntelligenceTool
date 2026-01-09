namespace AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

public record DealerId(Guid Value)
{
    public static DealerId CreateNew() => new(Guid.NewGuid());
}
