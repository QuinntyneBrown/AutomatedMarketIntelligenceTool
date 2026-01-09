namespace AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;

public record DeduplicationConfigId(Guid Value)
{
    public static DeduplicationConfigId CreateNew() => new(Guid.NewGuid());

    public static implicit operator Guid(DeduplicationConfigId id) => id.Value;

    public static implicit operator DeduplicationConfigId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
