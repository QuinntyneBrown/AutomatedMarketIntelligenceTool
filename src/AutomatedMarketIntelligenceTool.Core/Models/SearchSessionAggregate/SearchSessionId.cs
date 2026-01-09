namespace AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;

public record SearchSessionId(Guid Value)
{
    public static SearchSessionId Create() => new(Guid.NewGuid());
    public static SearchSessionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
