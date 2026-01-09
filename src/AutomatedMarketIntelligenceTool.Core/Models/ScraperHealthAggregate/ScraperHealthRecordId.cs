namespace AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;

public record ScraperHealthRecordId(Guid Value)
{
    public static ScraperHealthRecordId Create() => new(Guid.NewGuid());

    public static implicit operator Guid(ScraperHealthRecordId id) => id.Value;

    public static implicit operator ScraperHealthRecordId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
