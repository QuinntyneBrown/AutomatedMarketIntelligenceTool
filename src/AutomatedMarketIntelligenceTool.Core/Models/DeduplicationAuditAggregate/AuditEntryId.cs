namespace AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;

public record AuditEntryId(Guid Value)
{
    public static AuditEntryId CreateNew() => new(Guid.NewGuid());

    public static implicit operator Guid(AuditEntryId id) => id.Value;

    public static implicit operator AuditEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
