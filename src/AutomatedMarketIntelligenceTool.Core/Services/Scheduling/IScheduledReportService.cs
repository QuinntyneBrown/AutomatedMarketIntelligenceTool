using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Scheduling;

/// <summary>
/// Service for managing scheduled report configurations.
/// </summary>
public interface IScheduledReportService
{
    /// <summary>
    /// Creates a new scheduled report.
    /// </summary>
    Task<ScheduledReport> CreateScheduledReportAsync(
        Guid tenantId,
        string name,
        ReportFormat format,
        ReportSchedule schedule,
        TimeSpan scheduledTime,
        string outputDirectory,
        string? description = null,
        DayOfWeek? scheduledDayOfWeek = null,
        int? scheduledDayOfMonth = null,
        string? searchCriteriaJson = null,
        Guid? customMarketId = null,
        string? filenameTemplate = null,
        string? emailRecipients = null,
        bool includeStatistics = true,
        bool includePriceTrends = true,
        int maxListings = 1000,
        int retentionCount = 10,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a scheduled report by ID.
    /// </summary>
    Task<ScheduledReport?> GetScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scheduled reports for a tenant.
    /// </summary>
    Task<IReadOnlyList<ScheduledReport>> GetAllScheduledReportsAsync(
        Guid tenantId,
        ScheduledReportStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled reports that are due to run.
    /// </summary>
    Task<IReadOnlyList<ScheduledReport>> GetDueScheduledReportsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a scheduled report.
    /// </summary>
    Task<ScheduledReport> UpdateScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string name,
        ReportFormat format,
        ReportSchedule schedule,
        TimeSpan scheduledTime,
        string outputDirectory,
        string? description = null,
        DayOfWeek? scheduledDayOfWeek = null,
        int? scheduledDayOfMonth = null,
        string? searchCriteriaJson = null,
        Guid? customMarketId = null,
        string? filenameTemplate = null,
        string? emailRecipients = null,
        bool? includeStatistics = null,
        bool? includePriceTrends = null,
        int? maxListings = null,
        int? retentionCount = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a scheduled report.
    /// </summary>
    Task<bool> PauseScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused scheduled report.
    /// </summary>
    Task<bool> ResumeScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled report.
    /// </summary>
    Task<bool> CancelScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scheduled report.
    /// </summary>
    Task<bool> DeleteScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful run of a scheduled report.
    /// </summary>
    Task RecordSuccessfulRunAsync(
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed run of a scheduled report.
    /// </summary>
    Task RecordFailedRunAsync(
        ScheduledReportId scheduledReportId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a scheduled report immediately, regardless of its schedule.
    /// </summary>
    Task<Report> RunNowAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default);
}
