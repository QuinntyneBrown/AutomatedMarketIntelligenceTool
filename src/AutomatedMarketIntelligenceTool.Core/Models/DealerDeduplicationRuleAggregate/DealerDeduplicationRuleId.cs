namespace AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;

public record DealerDeduplicationRuleId(Guid Value)
{
    public static DealerDeduplicationRuleId CreateNew() => new(Guid.NewGuid());
}
