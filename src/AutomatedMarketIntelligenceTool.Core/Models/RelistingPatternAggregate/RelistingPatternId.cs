namespace AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;

public record RelistingPatternId(Guid Value)
{
    public static RelistingPatternId CreateNew() => new(Guid.NewGuid());
}
