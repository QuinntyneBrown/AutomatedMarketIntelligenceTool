using Reporting.Core.Enums;

namespace Reporting.Api.DTOs;

// Request DTOs
public sealed record GenerateReportRequest(
    string Name,
    string Type,
    ReportFormat Format,
    string? Parameters = null);

public sealed record CreateScheduledReportRequest(
    string Name,
    string ReportType,
    string CronExpression,
    ReportFormat Format,
    string? Parameters = null);

public sealed record UpdateScheduledReportRequest(
    string Name,
    string ReportType,
    string CronExpression,
    ReportFormat Format,
    string? Parameters = null);

// Response DTOs
public sealed record ReportResponse(
    Guid Id,
    string Name,
    string Type,
    string Status,
    ReportFormat Format,
    string? Parameters,
    string? FilePath,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Guid? RequestedBy);

public sealed record ScheduledReportResponse(
    Guid Id,
    string Name,
    string ReportType,
    string CronExpression,
    ReportFormat Format,
    string? Parameters,
    bool IsActive,
    DateTimeOffset? LastRunAt,
    DateTimeOffset? NextRunAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? OwnerId);

public sealed record ReportListResponse(
    IReadOnlyList<ReportResponse> Reports,
    int TotalCount);

public sealed record ScheduledReportListResponse(
    IReadOnlyList<ScheduledReportResponse> ScheduledReports,
    int TotalCount);
