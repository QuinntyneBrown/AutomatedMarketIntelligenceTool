namespace ScrapingOrchestration.Core.Enums;

/// <summary>
/// Status of a search session.
/// </summary>
public enum SearchSessionStatus
{
    /// <summary>
    /// Session is pending and has not started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Session is currently running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Session completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Session failed with errors.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Session was cancelled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Session is paused.
    /// </summary>
    Paused = 5
}
