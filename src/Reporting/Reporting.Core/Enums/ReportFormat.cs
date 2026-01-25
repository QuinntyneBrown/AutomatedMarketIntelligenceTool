namespace Reporting.Core.Enums;

/// <summary>
/// Supported report output formats.
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// Microsoft Excel format.
    /// </summary>
    Excel = 0,

    /// <summary>
    /// PDF document format.
    /// </summary>
    Pdf = 1,

    /// <summary>
    /// HTML format.
    /// </summary>
    Html = 2,

    /// <summary>
    /// CSV (Comma Separated Values) format.
    /// </summary>
    Csv = 3
}
