namespace AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;

public record ResourceThrottleId(Guid Value)
{
    public static ResourceThrottleId CreateNew() => new(Guid.NewGuid());

    public static implicit operator Guid(ResourceThrottleId id) => id.Value;

    public static implicit operator ResourceThrottleId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
