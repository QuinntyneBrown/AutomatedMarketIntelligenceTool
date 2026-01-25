namespace Deduplication.Core.Entities;

/// <summary>
/// Audit entry for deduplication operations.
/// </summary>
public sealed class AuditEntry
{
    public Guid Id { get; init; }
    public string Operation { get; init; } = string.Empty;
    public Guid? ListingId { get; init; }
    public Guid? DuplicateMatchId { get; init; }
    public Guid? ReviewItemId { get; init; }
    public string? Details { get; init; }
    public Guid? PerformedBy { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    private AuditEntry() { }

    public static AuditEntry Create(
        string operation,
        Guid? listingId = null,
        Guid? duplicateMatchId = null,
        Guid? reviewItemId = null,
        string? details = null,
        Guid? performedBy = null)
    {
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            Operation = operation,
            ListingId = listingId,
            DuplicateMatchId = duplicateMatchId,
            ReviewItemId = reviewItemId,
            Details = details,
            PerformedBy = performedBy,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
