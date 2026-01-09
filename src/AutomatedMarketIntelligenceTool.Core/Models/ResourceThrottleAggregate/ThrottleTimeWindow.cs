namespace AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;

/// <summary>
/// Defines the time window for throttle limits.
/// </summary>
public enum ThrottleTimeWindow
{
    /// <summary>
    /// Per second limit.
    /// </summary>
    PerSecond = 0,

    /// <summary>
    /// Per minute limit.
    /// </summary>
    PerMinute = 1,

    /// <summary>
    /// Per hour limit.
    /// </summary>
    PerHour = 2,

    /// <summary>
    /// Per day limit.
    /// </summary>
    PerDay = 3,

    /// <summary>
    /// Concurrent/simultaneous limit.
    /// </summary>
    Concurrent = 4
}
