namespace Deduplication.Core.Enums;

/// <summary>
/// Confidence level of a duplicate match.
/// </summary>
public enum MatchConfidence
{
    /// <summary>
    /// Very low confidence - unlikely to be a duplicate.
    /// </summary>
    VeryLow = 0,

    /// <summary>
    /// Low confidence - possible but unlikely duplicate.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium confidence - possible duplicate, needs review.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High confidence - likely duplicate.
    /// </summary>
    High = 3,

    /// <summary>
    /// Very high confidence - almost certainly a duplicate.
    /// </summary>
    VeryHigh = 4,

    /// <summary>
    /// Exact match - definitely the same listing.
    /// </summary>
    Exact = 5
}
