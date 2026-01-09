namespace AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;

public record ScheduledReportId(Guid Value)
{
    public static ScheduledReportId CreateNew() => new(Guid.NewGuid());

    public static implicit operator Guid(ScheduledReportId id) => id.Value;

    public static implicit operator ScheduledReportId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
