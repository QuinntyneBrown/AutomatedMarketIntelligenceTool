namespace AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;

public record ReviewItemId(Guid Value)
{
    public static ReviewItemId Create() => new(Guid.NewGuid());

    public static implicit operator Guid(ReviewItemId reviewItemId) => reviewItemId.Value;

    public static implicit operator ReviewItemId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
