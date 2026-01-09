namespace AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;

/// <summary>
/// Defines the frequency at which a scheduled report should be generated.
/// </summary>
public enum ReportSchedule
{
    /// <summary>
    /// Report is generated once and not repeated.
    /// </summary>
    Once = 0,

    /// <summary>
    /// Report is generated daily.
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Report is generated weekly.
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Report is generated bi-weekly (every two weeks).
    /// </summary>
    BiWeekly = 3,

    /// <summary>
    /// Report is generated monthly.
    /// </summary>
    Monthly = 4,

    /// <summary>
    /// Report is generated quarterly.
    /// </summary>
    Quarterly = 5
}
