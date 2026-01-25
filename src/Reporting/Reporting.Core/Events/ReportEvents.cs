using Reporting.Core.Enums;
using Shared.Contracts.Events;

namespace Reporting.Core.Events;

/// <summary>
/// Event raised when a report has been successfully generated.
/// </summary>
public sealed record ReportGeneratedEvent : IntegrationEvent
{
    public Guid ReportId { get; init; }
    public string ReportName { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public ReportFormat Format { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
    public Guid? RequestedBy { get; init; }
}

/// <summary>
/// Event raised when a report generation has failed.
/// </summary>
public sealed record ReportFailedEvent : IntegrationEvent
{
    public Guid ReportId { get; init; }
    public string ReportName { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public DateTimeOffset FailedAt { get; init; }
    public Guid? RequestedBy { get; init; }
}
