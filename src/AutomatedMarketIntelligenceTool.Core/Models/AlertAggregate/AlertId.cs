namespace AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;

public record AlertId(Guid Value)
{
    public static AlertId CreateNew() => new(Guid.NewGuid());
}
