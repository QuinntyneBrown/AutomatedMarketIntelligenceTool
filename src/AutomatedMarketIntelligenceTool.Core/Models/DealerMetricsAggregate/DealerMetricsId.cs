namespace AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;

public record DealerMetricsId(Guid Value)
{
    public static DealerMetricsId CreateNew() => new(Guid.NewGuid());
}
