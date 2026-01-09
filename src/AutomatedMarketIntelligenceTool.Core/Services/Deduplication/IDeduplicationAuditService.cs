using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

/// <summary>
/// Service for managing deduplication audit trail and tracking.
/// </summary>
public interface IDeduplicationAuditService
{
    /// <summary>
    /// Records an automatic deduplication decision.
    /// </summary>
    Task<AuditEntry> RecordAutomaticDecisionAsync(
        Guid tenantId,
        Guid listing1Id,
        Guid? listing2Id,
        AuditDecision decision,
        AuditReason reason,
        decimal? confidenceScore = null,
        string? fuzzyMatchDetailsJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a manual override of a previous decision.
    /// </summary>
    Task<AuditEntry> RecordManualOverrideAsync(
        Guid tenantId,
        Guid listing1Id,
        Guid? listing2Id,
        AuditDecision newDecision,
        AuditReason reason,
        string overrideReason,
        Guid originalAuditEntryId,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries for a specific listing.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetAuditEntriesForListingAsync(
        Guid tenantId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries with filtering options.
    /// </summary>
    Task<AuditQueryResult> QueryAuditEntriesAsync(
        Guid tenantId,
        AuditQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an audit entry as a false positive.
    /// </summary>
    Task<bool> MarkAsFalsePositiveAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an audit entry as a false negative.
    /// </summary>
    Task<bool> MarkAsFalseNegativeAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears false positive/negative flags from an audit entry.
    /// </summary>
    Task<bool> ClearErrorFlagsAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets false positive tracking statistics.
    /// </summary>
    Task<FalsePositiveStats> GetFalsePositiveStatsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an audit entry by ID.
    /// </summary>
    Task<AuditEntry?> GetByIdAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter options for querying audit entries.
/// </summary>
public class AuditQueryFilter
{
    public AuditDecision? Decision { get; init; }
    public AuditReason? Reason { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool? WasAutomatic { get; init; }
    public bool? HasManualOverride { get; init; }
    public bool? IsFalsePositive { get; init; }
    public bool? IsFalseNegative { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 100;
    public AuditSortField SortBy { get; init; } = AuditSortField.CreatedAt;
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Fields available for sorting audit entries.
/// </summary>
public enum AuditSortField
{
    CreatedAt,
    Decision,
    ConfidenceScore
}

/// <summary>
/// Result of an audit query with pagination info.
/// </summary>
public class AuditQueryResult
{
    public IReadOnlyList<AuditEntry> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
    public bool HasMore => Skip + Items.Count < TotalCount;
}

/// <summary>
/// Statistics about false positives and negatives.
/// </summary>
public class FalsePositiveStats
{
    public int TotalDecisions { get; init; }
    public int FalsePositiveCount { get; init; }
    public int FalseNegativeCount { get; init; }
    public int TruePositiveCount { get; init; }
    public int TrueNegativeCount { get; init; }

    /// <summary>
    /// False positive rate: FP / (FP + TN)
    /// </summary>
    public double FalsePositiveRate => TrueNegativeCount + FalsePositiveCount > 0
        ? (double)FalsePositiveCount / (FalsePositiveCount + TrueNegativeCount)
        : 0;

    /// <summary>
    /// False negative rate: FN / (FN + TP)
    /// </summary>
    public double FalseNegativeRate => TruePositiveCount + FalseNegativeCount > 0
        ? (double)FalseNegativeCount / (FalseNegativeCount + TruePositiveCount)
        : 0;

    /// <summary>
    /// Breakdown by decision type.
    /// </summary>
    public Dictionary<AuditDecision, int> DecisionBreakdown { get; init; } = new();

    /// <summary>
    /// Breakdown by reason/method.
    /// </summary>
    public Dictionary<AuditReason, int> ReasonBreakdown { get; init; } = new();
}
