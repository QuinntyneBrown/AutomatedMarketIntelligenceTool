using Microsoft.AspNetCore.Mvc;
using Reporting.Api.DTOs;
using Reporting.Core.Entities;
using Reporting.Core.Enums;
using Reporting.Core.Interfaces;

namespace Reporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IScheduledReportService _scheduledReportService;

    public ReportsController(
        IReportService reportService,
        IScheduledReportService scheduledReportService)
    {
        _reportService = reportService;
        _scheduledReportService = scheduledReportService;
    }

    [HttpPost]
    public async Task<ActionResult<ReportResponse>> GenerateReport(
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GenerateAsync(
            request.Name,
            request.Type,
            request.Format,
            request.Parameters,
            requestedBy: null, // Could be extracted from auth context
            cancellationToken);

        return CreatedAtAction(
            nameof(GetReport),
            new { id = report.Id },
            MapToResponse(report));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportResponse>> GetReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetByIdAsync(id, cancellationToken);
        if (report == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(report));
    }

    [HttpGet]
    public async Task<ActionResult<ReportListResponse>> GetReports(
        [FromQuery] ReportStatus? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var reports = await _reportService.GetAllAsync(status, type, skip, take, cancellationToken);
        var response = new ReportListResponse(
            reports.Select(MapToResponse).ToList(),
            reports.Count);

        return Ok(response);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetByIdAsync(id, cancellationToken);
        if (report == null)
        {
            return NotFound();
        }

        if (report.Status != ReportStatus.Completed)
        {
            return BadRequest("Report is not yet completed");
        }

        var stream = await _reportService.GetReportFileAsync(id, cancellationToken);
        if (stream == null)
        {
            return NotFound("Report file not found");
        }

        var contentType = report.Format switch
        {
            ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.Pdf => "application/pdf",
            ReportFormat.Html => "text/html",
            ReportFormat.Csv => "text/csv",
            _ => "application/octet-stream"
        };

        var extension = report.Format switch
        {
            ReportFormat.Excel => ".xlsx",
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Html => ".html",
            ReportFormat.Csv => ".csv",
            _ => ".txt"
        };

        return File(stream, contentType, $"{report.Name}{extension}");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _reportService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Scheduled Reports

    [HttpPost("scheduled")]
    public async Task<ActionResult<ScheduledReportResponse>> CreateScheduledReport(
        [FromBody] CreateScheduledReportRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledReport = await _scheduledReportService.CreateAsync(
            request.Name,
            request.ReportType,
            request.CronExpression,
            request.Format,
            request.Parameters,
            ownerId: null, // Could be extracted from auth context
            cancellationToken);

        return CreatedAtAction(
            nameof(GetScheduledReport),
            new { id = scheduledReport.Id },
            MapToScheduledResponse(scheduledReport));
    }

    [HttpGet("scheduled/{id:guid}")]
    public async Task<ActionResult<ScheduledReportResponse>> GetScheduledReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var scheduledReport = await _scheduledReportService.GetByIdAsync(id, cancellationToken);
        if (scheduledReport == null)
        {
            return NotFound();
        }

        return Ok(MapToScheduledResponse(scheduledReport));
    }

    [HttpGet("scheduled")]
    public async Task<ActionResult<ScheduledReportListResponse>> GetScheduledReports(
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var scheduledReports = await _scheduledReportService.GetScheduledReportsAsync(activeOnly, cancellationToken);
        var response = new ScheduledReportListResponse(
            scheduledReports.Select(MapToScheduledResponse).ToList(),
            scheduledReports.Count);

        return Ok(response);
    }

    [HttpPut("scheduled/{id:guid}")]
    public async Task<ActionResult<ScheduledReportResponse>> UpdateScheduledReport(
        Guid id,
        [FromBody] UpdateScheduledReportRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledReport = await _scheduledReportService.UpdateAsync(
            id,
            request.Name,
            request.ReportType,
            request.CronExpression,
            request.Format,
            request.Parameters,
            cancellationToken);

        if (scheduledReport == null)
        {
            return NotFound();
        }

        return Ok(MapToScheduledResponse(scheduledReport));
    }

    [HttpPost("scheduled/{id:guid}/activate")]
    public async Task<IActionResult> ActivateScheduledReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var activated = await _scheduledReportService.ActivateAsync(id, cancellationToken);
        if (!activated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("scheduled/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateScheduledReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deactivated = await _scheduledReportService.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("scheduled/{id:guid}")]
    public async Task<IActionResult> DeleteScheduledReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _scheduledReportService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static ReportResponse MapToResponse(Report report)
    {
        return new ReportResponse(
            report.Id,
            report.Name,
            report.Type,
            report.Status.ToString(),
            report.Format,
            report.Parameters,
            report.FilePath,
            report.ErrorMessage,
            report.CreatedAt,
            report.CompletedAt,
            report.RequestedBy);
    }

    private static ScheduledReportResponse MapToScheduledResponse(ScheduledReport scheduledReport)
    {
        return new ScheduledReportResponse(
            scheduledReport.Id,
            scheduledReport.Name,
            scheduledReport.ReportType,
            scheduledReport.CronExpression,
            scheduledReport.Format,
            scheduledReport.Parameters,
            scheduledReport.IsActive,
            scheduledReport.LastRunAt,
            scheduledReport.NextRunAt,
            scheduledReport.CreatedAt,
            scheduledReport.UpdatedAt,
            scheduledReport.OwnerId);
    }
}
