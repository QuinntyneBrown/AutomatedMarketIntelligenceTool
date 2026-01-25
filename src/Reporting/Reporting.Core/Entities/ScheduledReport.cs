using Reporting.Core.Enums;

namespace Reporting.Core.Entities;

/// <summary>
/// Represents a scheduled report configuration.
/// </summary>
public sealed class ScheduledReport
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public string CronExpression { get; init; } = string.Empty;
    public ReportFormat Format { get; init; }
    public string? Parameters { get; init; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? OwnerId { get; init; }

    private ScheduledReport() { }

    public static ScheduledReport Create(
        string name,
        string reportType,
        string cronExpression,
        ReportFormat format,
        string? parameters = null,
        Guid? ownerId = null)
    {
        return new ScheduledReport
        {
            Id = Guid.NewGuid(),
            Name = name,
            ReportType = reportType,
            CronExpression = cronExpression,
            Format = format,
            Parameters = parameters,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            OwnerId = ownerId
        };
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLastRun(DateTimeOffset runTime)
    {
        LastRunAt = runTime;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateNextRun(DateTimeOffset? nextRun)
    {
        NextRunAt = nextRun;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        string name,
        string reportType,
        string cronExpression,
        ReportFormat format,
        string? parameters)
    {
        // Note: Since these are init-only, we create a modified copy in the service
        // This method is for validation or future migration to settable properties
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
