namespace AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;

/// <summary>
/// Represents an audit trail entry for a deduplication decision.
/// </summary>
public class AuditEntry
{
    public AuditEntryId AuditEntryId { get; private set; } = null!;
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The listing that was evaluated for deduplication.
    /// </summary>
    public Guid Listing1Id { get; private set; }

    /// <summary>
    /// The existing listing that was matched against (null for new listings).
    /// </summary>
    public Guid? Listing2Id { get; private set; }

    /// <summary>
    /// The decision made (NewListing, Duplicate, NearMatch, ManualOverride).
    /// </summary>
    public AuditDecision Decision { get; private set; }

    /// <summary>
    /// The reason/method for the decision (VinMatch, FuzzyMatch, etc.).
    /// </summary>
    public AuditReason Reason { get; private set; }

    /// <summary>
    /// The confidence score for the match (0-100). Null for VIN/ExternalId matches.
    /// </summary>
    public decimal? ConfidenceScore { get; private set; }

    /// <summary>
    /// Whether the decision was made automatically.
    /// </summary>
    public bool WasAutomatic { get; private set; }

    /// <summary>
    /// Whether a manual override was applied.
    /// </summary>
    public bool ManualOverride { get; private set; }

    /// <summary>
    /// The reason for the manual override (if applicable).
    /// </summary>
    public string? OverrideReason { get; private set; }

    /// <summary>
    /// Reference to the original audit entry if this is an override.
    /// </summary>
    public Guid? OriginalAuditEntryId { get; private set; }

    /// <summary>
    /// Indicates if this decision was later found to be a false positive.
    /// </summary>
    public bool IsFalsePositive { get; private set; }

    /// <summary>
    /// Indicates if this decision was later found to be a false negative.
    /// </summary>
    public bool IsFalseNegative { get; private set; }

    /// <summary>
    /// JSON serialized fuzzy match details (field scores).
    /// </summary>
    public string? FuzzyMatchDetailsJson { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }

    private AuditEntry() { }

    /// <summary>
    /// Creates a new audit entry for an automatic deduplication decision.
    /// </summary>
    public static AuditEntry CreateAutomatic(
        Guid tenantId,
        Guid listing1Id,
        Guid? listing2Id,
        AuditDecision decision,
        AuditReason reason,
        decimal? confidenceScore = null,
        string? fuzzyMatchDetailsJson = null)
    {
        return new AuditEntry
        {
            AuditEntryId = AuditEntryId.CreateNew(),
            TenantId = tenantId,
            Listing1Id = listing1Id,
            Listing2Id = listing2Id,
            Decision = decision,
            Reason = reason,
            ConfidenceScore = confidenceScore,
            WasAutomatic = true,
            ManualOverride = false,
            FuzzyMatchDetailsJson = fuzzyMatchDetailsJson,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new audit entry for a manual override.
    /// </summary>
    public static AuditEntry CreateManualOverride(
        Guid tenantId,
        Guid listing1Id,
        Guid? listing2Id,
        AuditDecision newDecision,
        AuditReason reason,
        string overrideReason,
        Guid originalAuditEntryId,
        string createdBy)
    {
        return new AuditEntry
        {
            AuditEntryId = AuditEntryId.CreateNew(),
            TenantId = tenantId,
            Listing1Id = listing1Id,
            Listing2Id = listing2Id,
            Decision = newDecision,
            Reason = reason,
            WasAutomatic = false,
            ManualOverride = true,
            OverrideReason = overrideReason,
            OriginalAuditEntryId = originalAuditEntryId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Marks this audit entry as a false positive (incorrectly flagged as duplicate).
    /// </summary>
    public void MarkAsFalsePositive()
    {
        IsFalsePositive = true;
    }

    /// <summary>
    /// Marks this audit entry as a false negative (missed duplicate).
    /// </summary>
    public void MarkAsFalseNegative()
    {
        IsFalseNegative = true;
    }

    /// <summary>
    /// Clears the false positive/negative flags.
    /// </summary>
    public void ClearErrorFlags()
    {
        IsFalsePositive = false;
        IsFalseNegative = false;
    }
}
