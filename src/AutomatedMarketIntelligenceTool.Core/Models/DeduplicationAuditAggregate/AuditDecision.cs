namespace AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;

/// <summary>
/// The decision made during deduplication processing.
/// </summary>
public enum AuditDecision
{
    /// <summary>
    /// The listing was determined to be a new, unique listing.
    /// </summary>
    NewListing,

    /// <summary>
    /// The listing was determined to be a duplicate of an existing listing.
    /// </summary>
    Duplicate,

    /// <summary>
    /// The listing was flagged as a near-match requiring manual review.
    /// </summary>
    NearMatch,

    /// <summary>
    /// A manual override was applied to change a previous decision.
    /// </summary>
    ManualOverride
}
