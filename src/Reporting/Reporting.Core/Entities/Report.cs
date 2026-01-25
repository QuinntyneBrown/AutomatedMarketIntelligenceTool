using Reporting.Core.Enums;

namespace Reporting.Core.Entities;

/// <summary>
/// Represents a generated report.
/// </summary>
public sealed class Report
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public string? Parameters { get; init; }
    public string? FilePath { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ReportFormat Format { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? RequestedBy { get; init; }

    private Report() { }

    public static Report Create(
        string name,
        string type,
        ReportFormat format,
        string? parameters = null,
        Guid? requestedBy = null)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Format = format,
            Parameters = parameters,
            Status = ReportStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            RequestedBy = requestedBy
        };
    }

    public void MarkAsGenerating()
    {
        Status = ReportStatus.Generating;
    }

    public void MarkAsCompleted(string filePath)
    {
        Status = ReportStatus.Completed;
        FilePath = filePath;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ReportStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
