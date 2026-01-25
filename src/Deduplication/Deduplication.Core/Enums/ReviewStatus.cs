namespace Deduplication.Core.Enums;

/// <summary>
/// Status of a review item.
/// </summary>
public enum ReviewStatus
{
    /// <summary>
    /// Pending review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Confirmed as duplicate.
    /// </summary>
    ConfirmedDuplicate = 1,

    /// <summary>
    /// Confirmed as not a duplicate.
    /// </summary>
    ConfirmedNotDuplicate = 2,

    /// <summary>
    /// Skipped/deferred.
    /// </summary>
    Skipped = 3
}
