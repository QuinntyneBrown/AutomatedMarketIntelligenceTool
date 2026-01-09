namespace AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;

public record CustomMarketId(Guid Value)
{
    public static CustomMarketId CreateNew() => new(Guid.NewGuid());

    public static implicit operator Guid(CustomMarketId id) => id.Value;

    public static implicit operator CustomMarketId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
