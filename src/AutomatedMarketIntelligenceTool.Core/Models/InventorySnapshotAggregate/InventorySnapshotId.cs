namespace AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;

public record InventorySnapshotId(Guid Value)
{
    public static InventorySnapshotId CreateNew() => new(Guid.NewGuid());
}
