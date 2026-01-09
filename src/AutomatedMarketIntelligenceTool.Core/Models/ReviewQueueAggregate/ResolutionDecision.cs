namespace AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;

/// <summary>
/// The resolution decision for a review item.
/// </summary>
public enum ResolutionDecision
{
    /// <summary>
    /// No decision made yet.
    /// </summary>
    None = 0,

    /// <summary>
    /// User confirmed the listings represent the same vehicle.
    /// </summary>
    SameVehicle = 1,

    /// <summary>
    /// User confirmed the listings represent different vehicles.
    /// </summary>
    DifferentVehicle = 2
}

/// <summary>
/// The status of a review item.
/// </summary>
public enum ReviewItemStatus
{
    /// <summary>
    /// Pending review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Resolved by user.
    /// </summary>
    Resolved = 1,

    /// <summary>
    /// Dismissed without resolution.
    /// </summary>
    Dismissed = 2
}

/// <summary>
/// The method used to identify the match.
/// </summary>
public enum MatchMethod
{
    /// <summary>
    /// Fuzzy field matching.
    /// </summary>
    Fuzzy = 1,

    /// <summary>
    /// Image-based matching.
    /// </summary>
    Image = 2,

    /// <summary>
    /// Combined fuzzy and image matching.
    /// </summary>
    Combined = 3
}
