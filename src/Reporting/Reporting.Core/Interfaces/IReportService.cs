using Reporting.Core.Entities;
using Reporting.Core.Enums;

namespace Reporting.Core.Interfaces;

/// <summary>
/// Service for generating and managing reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a new report.
    /// </summary>
    Task<Report> GenerateAsync(
        string name,
        string type,
        ReportFormat format,
        string? parameters = null,
        Guid? requestedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a report by its ID.
    /// </summary>
    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reports with optional filtering.
    /// </summary>
    Task<IReadOnlyList<Report>> GetAllAsync(
        ReportStatus? status = null,
        string? type = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reports by their status.
    /// </summary>
    Task<IReadOnlyList<Report>> GetByStatusAsync(
        ReportStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a report by its ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file stream for a completed report.
    /// </summary>
    Task<Stream?> GetReportFileAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing scheduled reports.
/// </summary>
public interface IScheduledReportService
{
    /// <summary>
    /// Creates a new scheduled report.
    /// </summary>
    Task<ScheduledReport> CreateAsync(
        string name,
        string reportType,
        string cronExpression,
        ReportFormat format,
        string? parameters = null,
        Guid? ownerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a scheduled report by its ID.
    /// </summary>
    Task<ScheduledReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scheduled reports.
    /// </summary>
    Task<IReadOnlyList<ScheduledReport>> GetScheduledReportsAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a scheduled report.
    /// </summary>
    Task<ScheduledReport?> UpdateAsync(
        Guid id,
        string name,
        string reportType,
        string cronExpression,
        ReportFormat format,
        string? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a scheduled report.
    /// </summary>
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a scheduled report.
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scheduled report.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled reports that are due for execution.
    /// </summary>
    Task<IReadOnlyList<ScheduledReport>> GetDueReportsAsync(CancellationToken cancellationToken = default);
}
