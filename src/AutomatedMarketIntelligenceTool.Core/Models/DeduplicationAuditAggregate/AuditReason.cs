namespace AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;

/// <summary>
/// The reason or method used to arrive at a deduplication decision.
/// </summary>
public enum AuditReason
{
    /// <summary>
    /// No match was found - listing is unique.
    /// </summary>
    NoMatch,

    /// <summary>
    /// VIN matched exactly with an existing listing.
    /// </summary>
    VinMatch,

    /// <summary>
    /// External ID and source site matched an existing listing.
    /// </summary>
    ExternalIdMatch,

    /// <summary>
    /// Fuzzy matching determined the listing is a duplicate.
    /// </summary>
    FuzzyMatch,

    /// <summary>
    /// Image perceptual hashing matched an existing listing.
    /// </summary>
    ImageMatch,

    /// <summary>
    /// Combined fuzzy and image matching found a duplicate.
    /// </summary>
    CombinedMatch,

    /// <summary>
    /// Manual review resulted in the decision.
    /// </summary>
    ManualReview,

    /// <summary>
    /// A false positive was identified and corrected.
    /// </summary>
    FalsePositiveCorrection,

    /// <summary>
    /// A false negative was identified and corrected.
    /// </summary>
    FalseNegativeCorrection
}
