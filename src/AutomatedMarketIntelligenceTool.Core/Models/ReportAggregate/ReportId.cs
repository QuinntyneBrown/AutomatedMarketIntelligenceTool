namespace AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;

public record ReportId(Guid Value)
{
    public static ReportId CreateNew() => new(Guid.NewGuid());
}
