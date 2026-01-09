using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutomatedMarketIntelligenceTool.Core.Services.Scheduling;

/// <summary>
/// Service for managing scheduled report configurations.
/// </summary>
public class ScheduledReportService : IScheduledReportService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly ISearchService _searchService;
    private readonly ILogger<ScheduledReportService> _logger;

    public ScheduledReportService(
        IAutomatedMarketIntelligenceToolContext context,
        IReportGenerationService reportGenerationService,
        ISearchService searchService,
        ILogger<ScheduledReportService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _reportGenerationService = reportGenerationService ?? throw new ArgumentNullException(nameof(reportGenerationService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScheduledReport> CreateScheduledReportAsync(
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
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate name
        var existing = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Name == name.Trim(),
                cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"A scheduled report with name '{name}' already exists");
        }

        var scheduledReport = ScheduledReport.Create(
            tenantId,
            name,
            format,
            schedule,
            scheduledTime,
            outputDirectory,
            description,
            scheduledDayOfWeek,
            scheduledDayOfMonth,
            searchCriteriaJson,
            customMarketId,
            filenameTemplate,
            emailRecipients,
            includeStatistics,
            includePriceTrends,
            maxListings,
            retentionCount,
            createdBy);

        _context.ScheduledReports.Add(scheduledReport);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created scheduled report {ScheduledReportId} '{Name}' for tenant {TenantId} with schedule {Schedule}",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name, tenantId, schedule);

        return scheduledReport;
    }

    public async Task<ScheduledReport?> GetScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetAllScheduledReportsAsync(
        Guid tenantId,
        ScheduledReportStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ScheduledReports
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetDueScheduledReportsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.ScheduledReports
            .IgnoreQueryFilters()
            .Where(r => r.Status == ScheduledReportStatus.Active &&
                       r.NextRunAt != null &&
                       r.NextRunAt <= now)
            .OrderBy(r => r.NextRunAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledReport> UpdateScheduledReportAsync(
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
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
        {
            throw new InvalidOperationException($"Scheduled report {scheduledReportId} not found");
        }

        // Check for duplicate name (excluding current report)
        var duplicate = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Name == name.Trim() && r.ScheduledReportId != scheduledReportId,
                cancellationToken);

        if (duplicate != null)
        {
            throw new InvalidOperationException($"A scheduled report with name '{name}' already exists");
        }

        scheduledReport.Update(
            name,
            format,
            schedule,
            scheduledTime,
            outputDirectory,
            description,
            scheduledDayOfWeek,
            scheduledDayOfMonth,
            searchCriteriaJson,
            customMarketId,
            filenameTemplate,
            emailRecipients,
            includeStatistics,
            includePriceTrends,
            maxListings,
            retentionCount,
            updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated scheduled report {ScheduledReportId} '{Name}' for tenant {TenantId}",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name, tenantId);

        return scheduledReport;
    }

    public async Task<bool> PauseScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
            return false;

        scheduledReport.Pause(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Paused scheduled report {ScheduledReportId} '{Name}' for tenant {TenantId}",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name, tenantId);

        return true;
    }

    public async Task<bool> ResumeScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
            return false;

        scheduledReport.Resume(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Resumed scheduled report {ScheduledReportId} '{Name}' for tenant {TenantId}",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name, tenantId);

        return true;
    }

    public async Task<bool> CancelScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
            return false;

        scheduledReport.Cancel(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cancelled scheduled report {ScheduledReportId} '{Name}' for tenant {TenantId}",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name, tenantId);

        return true;
    }

    public async Task<bool> DeleteScheduledReportAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
            return false;

        _context.ScheduledReports.Remove(scheduledReport);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted scheduled report {ScheduledReportId} '{Name}' for tenant {TenantId}",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name, tenantId);

        return true;
    }

    public async Task RecordSuccessfulRunAsync(
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
        {
            throw new InvalidOperationException($"Scheduled report {scheduledReportId} not found");
        }

        scheduledReport.RecordSuccessfulRun(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailedRunAsync(
        ScheduledReportId scheduledReportId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
        {
            throw new InvalidOperationException($"Scheduled report {scheduledReportId} not found");
        }

        scheduledReport.RecordFailedRun(DateTime.UtcNow, errorMessage);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Report> RunNowAsync(
        Guid tenantId,
        ScheduledReportId scheduledReportId,
        CancellationToken cancellationToken = default)
    {
        var scheduledReport = await _context.ScheduledReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.ScheduledReportId == scheduledReportId,
                cancellationToken);

        if (scheduledReport == null)
        {
            throw new InvalidOperationException($"Scheduled report {scheduledReportId} not found");
        }

        _logger.LogInformation(
            "Running scheduled report {ScheduledReportId} '{Name}' now",
            scheduledReport.ScheduledReportId.Value, scheduledReport.Name);

        try
        {
            // Build search criteria from JSON
            var searchCriteria = new SearchCriteria();
            if (!string.IsNullOrWhiteSpace(scheduledReport.SearchCriteriaJson))
            {
                searchCriteria = JsonSerializer.Deserialize<SearchCriteria>(scheduledReport.SearchCriteriaJson)
                    ?? new SearchCriteria();
            }

            // Get listings based on search criteria
            var listings = await _searchService.SearchAsync(tenantId, searchCriteria, cancellationToken);
            var listingsToInclude = listings.Take(scheduledReport.MaxListings).ToList();

            // Build report data
            var reportData = new ReportData
            {
                Title = scheduledReport.Name,
                GeneratedAt = DateTime.UtcNow,
                Listings = listingsToInclude,
                SearchCriteria = searchCriteria,
                IncludeStatistics = scheduledReport.IncludeStatistics,
                IncludePriceTrends = scheduledReport.IncludePriceTrends,
                TotalListingsFound = listings.Count
            };

            // Ensure output directory exists
            if (!Directory.Exists(scheduledReport.OutputDirectory))
            {
                Directory.CreateDirectory(scheduledReport.OutputDirectory);
            }

            var filename = scheduledReport.GetOutputFilename();
            var outputPath = Path.Combine(scheduledReport.OutputDirectory, filename);

            var report = await _reportGenerationService.GenerateReportAsync(
                tenantId,
                scheduledReport.Name,
                scheduledReport.Format,
                reportData,
                outputPath,
                scheduledReport.SearchCriteriaJson,
                cancellationToken);

            scheduledReport.RecordSuccessfulRun(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            // Clean up old reports based on retention policy
            await CleanupOldReportsAsync(scheduledReport, cancellationToken);

            _logger.LogInformation(
                "Successfully ran scheduled report {ScheduledReportId} '{Name}', output: {OutputPath}",
                scheduledReport.ScheduledReportId.Value, scheduledReport.Name, outputPath);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to run scheduled report {ScheduledReportId} '{Name}'",
                scheduledReport.ScheduledReportId.Value, scheduledReport.Name);

            scheduledReport.RecordFailedRun(DateTime.UtcNow, ex.Message);
            await _context.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    private async Task CleanupOldReportsAsync(ScheduledReport scheduledReport, CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(scheduledReport.OutputDirectory))
                return;

            var extension = scheduledReport.Format switch
            {
                ReportFormat.Html => "*.html",
                ReportFormat.Pdf => "*.pdf",
                ReportFormat.Excel => "*.xlsx",
                _ => "*.*"
            };

            var sanitizedName = string.Join("_", scheduledReport.Name.Split(Path.GetInvalidFileNameChars()));
            var pattern = $"{sanitizedName}*{extension.TrimStart('*')}";

            var files = Directory.GetFiles(scheduledReport.OutputDirectory, pattern)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(scheduledReport.RetentionCount)
                .ToList();

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                    _logger.LogDebug("Deleted old report file: {FilePath}", file.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old report file: {FilePath}", file.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old reports for scheduled report {ScheduledReportId}",
                scheduledReport.ScheduledReportId.Value);
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Search criteria for scheduled reports.
/// </summary>
public class SearchCriteria
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public int? MileageMin { get; set; }
    public int? MileageMax { get; set; }
    public string? PostalCode { get; set; }
    public int? RadiusKm { get; set; }
    public string? BodyStyle { get; set; }
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public string? Condition { get; set; }
}
