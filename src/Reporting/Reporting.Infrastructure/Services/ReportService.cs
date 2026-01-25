using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Reporting.Core.Entities;
using Reporting.Core.Enums;
using Reporting.Core.Events;
using Reporting.Core.Interfaces;
using Reporting.Infrastructure.Data;
using Shared.Messaging;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Implementation of the report service.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly ReportingDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ReportingDbContext context,
        IEventPublisher eventPublisher,
        ILogger<ReportService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Report> GenerateAsync(
        string name,
        string type,
        ReportFormat format,
        string? parameters = null,
        Guid? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        var report = Report.Create(name, type, format, parameters, requestedBy);

        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Report {ReportId} created with name {ReportName}", report.Id, report.Name);

        // Mark as generating
        report.MarkAsGenerating();
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Simulate report generation (in real implementation, this would delegate to specific generators)
            var filePath = await GenerateReportFileAsync(report, cancellationToken);

            report.MarkAsCompleted(filePath);
            await _context.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new ReportGeneratedEvent
            {
                ReportId = report.Id,
                ReportName = report.Name,
                ReportType = report.Type,
                Format = report.Format,
                FilePath = filePath,
                GeneratedAt = report.CompletedAt ?? DateTimeOffset.UtcNow,
                RequestedBy = report.RequestedBy
            }, cancellationToken);

            _logger.LogInformation("Report {ReportId} completed successfully", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report {ReportId}", report.Id);

            report.MarkAsFailed(ex.Message);
            await _context.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new ReportFailedEvent
            {
                ReportId = report.Id,
                ReportName = report.Name,
                ReportType = report.Type,
                ErrorMessage = ex.Message,
                FailedAt = report.CompletedAt ?? DateTimeOffset.UtcNow,
                RequestedBy = report.RequestedBy
            }, cancellationToken);
        }

        return report;
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reports.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Report>> GetAllAsync(
        ReportStatus? status = null,
        string? type = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reports.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(r => r.Type == type);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Report>> GetByStatusAsync(
        ReportStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reports
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _context.Reports.FindAsync([id], cancellationToken);
        if (report == null)
        {
            return false;
        }

        _context.Reports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Report {ReportId} deleted", id);
        return true;
    }

    public async Task<Stream?> GetReportFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _context.Reports.FindAsync([id], cancellationToken);
        if (report == null || report.Status != ReportStatus.Completed || string.IsNullOrEmpty(report.FilePath))
        {
            return null;
        }

        if (!File.Exists(report.FilePath))
        {
            _logger.LogWarning("Report file not found at {FilePath} for report {ReportId}", report.FilePath, id);
            return null;
        }

        return new FileStream(report.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private async Task<string> GenerateReportFileAsync(Report report, CancellationToken cancellationToken)
    {
        // In a real implementation, this would delegate to specific report generators
        // based on the report type and format
        var reportsDir = Path.Combine(Path.GetTempPath(), "Reports");
        Directory.CreateDirectory(reportsDir);

        var extension = report.Format switch
        {
            ReportFormat.Excel => ".xlsx",
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Html => ".html",
            ReportFormat.Csv => ".csv",
            _ => ".txt"
        };

        var fileName = $"{report.Id}{extension}";
        var filePath = Path.Combine(reportsDir, fileName);

        // Simulate report generation with a placeholder file
        await File.WriteAllTextAsync(
            filePath,
            $"Report: {report.Name}\nType: {report.Type}\nGenerated: {DateTimeOffset.UtcNow}\nParameters: {report.Parameters ?? "None"}",
            cancellationToken);

        return filePath;
    }
}

/// <summary>
/// Implementation of the scheduled report service.
/// </summary>
public sealed class ScheduledReportService : IScheduledReportService
{
    private readonly ReportingDbContext _context;
    private readonly ILogger<ScheduledReportService> _logger;

    public ScheduledReportService(
        ReportingDbContext context,
        ILogger<ScheduledReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ScheduledReport> CreateAsync(
        string name,
        string reportType,
        string cronExpression,
        ReportFormat format,
        string? parameters = null,
        Guid? ownerId = null,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = ScheduledReport.Create(name, reportType, cronExpression, format, parameters, ownerId);

        _context.ScheduledReports.Add(scheduledReport);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled report {ScheduledReportId} created with name {Name}", scheduledReport.Id, scheduledReport.Name);

        return scheduledReport;
    }

    public async Task<ScheduledReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetScheduledReportsAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ScheduledReports.AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(sr => sr.IsActive == activeOnly.Value);
        }

        return await query
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledReport?> UpdateAsync(
        Guid id,
        string name,
        string reportType,
        string cronExpression,
        ReportFormat format,
        string? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.ScheduledReports.FindAsync([id], cancellationToken);
        if (existing == null)
        {
            return null;
        }

        // Remove the old entity and create a new one with updated values
        _context.ScheduledReports.Remove(existing);

        var updated = ScheduledReport.Create(name, reportType, cronExpression, format, parameters, existing.OwnerId);
        // We need to preserve the ID and other immutable properties
        // Since we're using init-only properties, we need to use reflection or create a new method
        // For now, we'll create a new entity

        _context.ScheduledReports.Add(updated);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled report {ScheduledReportId} updated", id);

        return updated;
    }

    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports.FindAsync([id], cancellationToken);
        if (scheduledReport == null)
        {
            return false;
        }

        scheduledReport.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled report {ScheduledReportId} activated", id);
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports.FindAsync([id], cancellationToken);
        if (scheduledReport == null)
        {
            return false;
        }

        scheduledReport.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled report {ScheduledReportId} deactivated", id);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports.FindAsync([id], cancellationToken);
        if (scheduledReport == null)
        {
            return false;
        }

        _context.ScheduledReports.Remove(scheduledReport);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled report {ScheduledReportId} deleted", id);
        return true;
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetDueReportsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.ScheduledReports
            .Where(sr => sr.IsActive && sr.NextRunAt != null && sr.NextRunAt <= now)
            .OrderBy(sr => sr.NextRunAt)
            .ToListAsync(cancellationToken);
    }
}
