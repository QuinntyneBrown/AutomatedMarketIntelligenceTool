namespace Reporting.Core.Enums;

/// <summary>
/// Status of a report generation process.
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Report is pending generation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Report is currently being generated.
    /// </summary>
    Generating = 1,

    /// <summary>
    /// Report has been successfully completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Report generation failed.
    /// </summary>
    Failed = 3
}
