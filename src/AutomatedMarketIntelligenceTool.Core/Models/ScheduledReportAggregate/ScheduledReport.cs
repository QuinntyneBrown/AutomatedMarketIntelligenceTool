using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;

/// <summary>
/// Represents a scheduled report configuration for automated report generation.
/// </summary>
public class ScheduledReport
{
    public ScheduledReportId ScheduledReportId { get; private set; } = null!;
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The name of the scheduled report.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The format for generated reports (HTML, PDF, Excel).
    /// </summary>
    public ReportFormat Format { get; private set; }

    /// <summary>
    /// The schedule frequency.
    /// </summary>
    public ReportSchedule Schedule { get; private set; }

    /// <summary>
    /// The time of day to generate the report (in UTC).
    /// </summary>
    public TimeSpan ScheduledTime { get; private set; }

    /// <summary>
    /// For weekly schedules, the day of week to generate the report.
    /// </summary>
    public DayOfWeek? ScheduledDayOfWeek { get; private set; }

    /// <summary>
    /// For monthly schedules, the day of month to generate the report (1-28).
    /// </summary>
    public int? ScheduledDayOfMonth { get; private set; }

    /// <summary>
    /// JSON containing the search criteria for the report.
    /// </summary>
    public string? SearchCriteriaJson { get; private set; }

    /// <summary>
    /// The custom market ID to use for the report, if any.
    /// </summary>
    public Guid? CustomMarketId { get; private set; }

    /// <summary>
    /// The output directory for generated reports.
    /// </summary>
    public string OutputDirectory { get; private set; } = null!;

    /// <summary>
    /// Optional filename template (e.g., "report-{date:yyyyMMdd}").
    /// </summary>
    public string? FilenameTemplate { get; private set; }

    /// <summary>
    /// The current status of the scheduled report.
    /// </summary>
    public ScheduledReportStatus Status { get; private set; }

    /// <summary>
    /// The last time the report was generated.
    /// </summary>
    public DateTime? LastRunAt { get; private set; }

    /// <summary>
    /// The next scheduled run time.
    /// </summary>
    public DateTime? NextRunAt { get; private set; }

    /// <summary>
    /// The number of successful runs.
    /// </summary>
    public int SuccessfulRuns { get; private set; }

    /// <summary>
    /// The number of failed runs.
    /// </summary>
    public int FailedRuns { get; private set; }

    /// <summary>
    /// The last error message if status is Error.
    /// </summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>
    /// Optional email addresses to send the report to (comma-separated).
    /// </summary>
    public string? EmailRecipients { get; private set; }

    /// <summary>
    /// Whether to include statistics in the report.
    /// </summary>
    public bool IncludeStatistics { get; private set; }

    /// <summary>
    /// Whether to include price trends in the report.
    /// </summary>
    public bool IncludePriceTrends { get; private set; }

    /// <summary>
    /// Maximum number of listings to include in the report.
    /// </summary>
    public int MaxListings { get; private set; }

    /// <summary>
    /// Number of reports to retain (older reports are automatically deleted).
    /// </summary>
    public int RetentionCount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private ScheduledReport() { }

    public static ScheduledReport Create(
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
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Schedule name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be empty", nameof(outputDirectory));

        ValidateScheduleParameters(schedule, scheduledDayOfWeek, scheduledDayOfMonth);

        var report = new ScheduledReport
        {
            ScheduledReportId = ScheduledReportId.CreateNew(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Format = format,
            Schedule = schedule,
            ScheduledTime = scheduledTime,
            ScheduledDayOfWeek = scheduledDayOfWeek,
            ScheduledDayOfMonth = scheduledDayOfMonth,
            SearchCriteriaJson = searchCriteriaJson,
            CustomMarketId = customMarketId,
            OutputDirectory = outputDirectory.Trim(),
            FilenameTemplate = filenameTemplate?.Trim(),
            Status = ScheduledReportStatus.Active,
            EmailRecipients = emailRecipients?.Trim(),
            IncludeStatistics = includeStatistics,
            IncludePriceTrends = includePriceTrends,
            MaxListings = maxListings,
            RetentionCount = retentionCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        report.CalculateNextRunTime();
        return report;
    }

    private static void ValidateScheduleParameters(
        ReportSchedule schedule,
        DayOfWeek? scheduledDayOfWeek,
        int? scheduledDayOfMonth)
    {
        if (schedule == ReportSchedule.Weekly && !scheduledDayOfWeek.HasValue)
        {
            throw new ArgumentException("Day of week is required for weekly schedules");
        }

        if (schedule == ReportSchedule.BiWeekly && !scheduledDayOfWeek.HasValue)
        {
            throw new ArgumentException("Day of week is required for bi-weekly schedules");
        }

        if ((schedule == ReportSchedule.Monthly || schedule == ReportSchedule.Quarterly)
            && !scheduledDayOfMonth.HasValue)
        {
            throw new ArgumentException("Day of month is required for monthly and quarterly schedules");
        }

        if (scheduledDayOfMonth.HasValue && (scheduledDayOfMonth.Value < 1 || scheduledDayOfMonth.Value > 28))
        {
            throw new ArgumentException("Day of month must be between 1 and 28");
        }
    }

    public void Update(
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
        string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Schedule name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be empty", nameof(outputDirectory));

        ValidateScheduleParameters(schedule, scheduledDayOfWeek, scheduledDayOfMonth);

        Name = name.Trim();
        Description = description?.Trim();
        Format = format;
        Schedule = schedule;
        ScheduledTime = scheduledTime;
        ScheduledDayOfWeek = scheduledDayOfWeek;
        ScheduledDayOfMonth = scheduledDayOfMonth;
        SearchCriteriaJson = searchCriteriaJson;
        CustomMarketId = customMarketId;
        OutputDirectory = outputDirectory.Trim();
        FilenameTemplate = filenameTemplate?.Trim();
        EmailRecipients = emailRecipients?.Trim();

        if (includeStatistics.HasValue)
            IncludeStatistics = includeStatistics.Value;
        if (includePriceTrends.HasValue)
            IncludePriceTrends = includePriceTrends.Value;
        if (maxListings.HasValue)
            MaxListings = maxListings.Value;
        if (retentionCount.HasValue)
            RetentionCount = retentionCount.Value;

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        CalculateNextRunTime();
    }

    public void Pause(string? updatedBy = null)
    {
        Status = ScheduledReportStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Resume(string? updatedBy = null)
    {
        Status = ScheduledReportStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        CalculateNextRunTime();
    }

    public void Cancel(string? updatedBy = null)
    {
        Status = ScheduledReportStatus.Cancelled;
        NextRunAt = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void RecordSuccessfulRun(DateTime runTime)
    {
        LastRunAt = runTime;
        SuccessfulRuns++;
        LastErrorMessage = null;

        if (Schedule == ReportSchedule.Once)
        {
            Status = ScheduledReportStatus.Completed;
            NextRunAt = null;
        }
        else if (Status == ScheduledReportStatus.Error)
        {
            Status = ScheduledReportStatus.Active;
        }

        UpdatedAt = DateTime.UtcNow;
        CalculateNextRunTime();
    }

    public void RecordFailedRun(DateTime runTime, string errorMessage)
    {
        LastRunAt = runTime;
        FailedRuns++;
        LastErrorMessage = errorMessage;
        Status = ScheduledReportStatus.Error;
        UpdatedAt = DateTime.UtcNow;
        CalculateNextRunTime();
    }

    public void CalculateNextRunTime()
    {
        if (Status == ScheduledReportStatus.Paused ||
            Status == ScheduledReportStatus.Cancelled ||
            Status == ScheduledReportStatus.Completed)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = now.Date;
        var scheduledDateTime = today.Add(ScheduledTime);

        NextRunAt = Schedule switch
        {
            ReportSchedule.Once => LastRunAt == null ? GetNextOccurrence(scheduledDateTime, now) : null,
            ReportSchedule.Daily => GetNextDailyOccurrence(scheduledDateTime, now),
            ReportSchedule.Weekly => GetNextWeeklyOccurrence(now),
            ReportSchedule.BiWeekly => GetNextBiWeeklyOccurrence(now),
            ReportSchedule.Monthly => GetNextMonthlyOccurrence(now),
            ReportSchedule.Quarterly => GetNextQuarterlyOccurrence(now),
            _ => null
        };
    }

    private static DateTime GetNextOccurrence(DateTime scheduledDateTime, DateTime now)
    {
        return scheduledDateTime <= now ? scheduledDateTime.AddDays(1) : scheduledDateTime;
    }

    private DateTime GetNextDailyOccurrence(DateTime scheduledDateTime, DateTime now)
    {
        return scheduledDateTime <= now ? scheduledDateTime.AddDays(1) : scheduledDateTime;
    }

    private DateTime GetNextWeeklyOccurrence(DateTime now)
    {
        if (!ScheduledDayOfWeek.HasValue)
            return now.AddDays(1);

        var daysUntilTarget = ((int)ScheduledDayOfWeek.Value - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0 && now.TimeOfDay >= ScheduledTime)
            daysUntilTarget = 7;

        return now.Date.AddDays(daysUntilTarget).Add(ScheduledTime);
    }

    private DateTime GetNextBiWeeklyOccurrence(DateTime now)
    {
        var weekly = GetNextWeeklyOccurrence(now);

        // If last run was within the past week, add another week
        if (LastRunAt.HasValue && (now - LastRunAt.Value).TotalDays < 7)
        {
            return weekly.AddDays(7);
        }

        return weekly;
    }

    private DateTime GetNextMonthlyOccurrence(DateTime now)
    {
        if (!ScheduledDayOfMonth.HasValue)
            return now.AddDays(1);

        var day = ScheduledDayOfMonth.Value;
        var targetDate = new DateTime(now.Year, now.Month, day, 0, 0, 0, DateTimeKind.Utc).Add(ScheduledTime);

        if (targetDate <= now)
        {
            targetDate = targetDate.AddMonths(1);
        }

        return targetDate;
    }

    private DateTime GetNextQuarterlyOccurrence(DateTime now)
    {
        if (!ScheduledDayOfMonth.HasValue)
            return now.AddDays(1);

        var day = ScheduledDayOfMonth.Value;
        var currentQuarter = (now.Month - 1) / 3;
        var quarterStartMonth = currentQuarter * 3 + 1;
        var targetDate = new DateTime(now.Year, quarterStartMonth, day, 0, 0, 0, DateTimeKind.Utc).Add(ScheduledTime);

        if (targetDate <= now)
        {
            // Move to next quarter
            targetDate = targetDate.AddMonths(3);
        }

        return targetDate;
    }

    public string GetOutputFilename()
    {
        var template = FilenameTemplate ?? "{name}-{date:yyyyMMdd-HHmmss}";
        var now = DateTime.UtcNow;

        var filename = template
            .Replace("{name}", SanitizeFilename(Name))
            .Replace("{date:yyyyMMdd-HHmmss}", now.ToString("yyyyMMdd-HHmmss"))
            .Replace("{date:yyyyMMdd}", now.ToString("yyyyMMdd"))
            .Replace("{date}", now.ToString("yyyy-MM-dd"));

        var extension = Format switch
        {
            ReportFormat.Html => ".html",
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Excel => ".xlsx",
            _ => ".html"
        };

        return filename + extension;
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    public bool IsDue()
    {
        return Status == ScheduledReportStatus.Active &&
               NextRunAt.HasValue &&
               NextRunAt.Value <= DateTime.UtcNow;
    }

    public IReadOnlyList<string> GetEmailRecipientList()
    {
        if (string.IsNullOrWhiteSpace(EmailRecipients))
            return Array.Empty<string>();

        return EmailRecipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
