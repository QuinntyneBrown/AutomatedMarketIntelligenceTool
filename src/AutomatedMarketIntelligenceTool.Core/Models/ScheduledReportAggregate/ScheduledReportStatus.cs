namespace AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;

/// <summary>
/// Defines the status of a scheduled report.
/// </summary>
public enum ScheduledReportStatus
{
    /// <summary>
    /// The scheduled report is active and will generate reports.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The scheduled report is paused and will not generate reports until resumed.
    /// </summary>
    Paused = 1,

    /// <summary>
    /// The scheduled report has been completed (for Once schedule) or expired.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The scheduled report has been cancelled.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// The scheduled report encountered an error during last execution.
    /// </summary>
    Error = 4
}
