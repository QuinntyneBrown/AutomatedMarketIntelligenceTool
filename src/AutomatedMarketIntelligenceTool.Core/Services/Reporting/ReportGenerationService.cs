using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Reporting;

public class ReportGenerationService : IReportGenerationService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IEnumerable<IReportGenerator> _generators;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(
        IAutomatedMarketIntelligenceToolContext context,
        IEnumerable<IReportGenerator> generators,
        ILogger<ReportGenerationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _generators = generators ?? throw new ArgumentNullException(nameof(generators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Report> GenerateReportAsync(
        Guid tenantId,
        string name,
        ReportFormat format,
        ReportData data,
        string outputPath,
        string? searchCriteriaJson = null,
        CancellationToken cancellationToken = default)
    {
        var report = Report.Create(tenantId, name, format, searchCriteriaJson);
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            report.MarkAsGenerating();
            await _context.SaveChangesAsync(cancellationToken);

            var generator = _generators.FirstOrDefault(g => g.SupportedFormat == format);
            if (generator == null)
            {
                throw new InvalidOperationException($"No generator found for format: {format}");
            }

            var filePath = await generator.GenerateReportAsync(data, outputPath, cancellationToken);
            var fileInfo = new FileInfo(filePath);

            report.MarkAsComplete(filePath, fileInfo.Length);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Report {ReportId} generated successfully at {FilePath}", 
                report.ReportId.Value, filePath);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report {ReportId}", report.ReportId.Value);
            report.MarkAsFailed(ex.Message);
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<string> GetReportPathAsync(ReportId reportId, CancellationToken cancellationToken = default)
    {
        var report = await _context.Reports
            .FindAsync(new object[] { reportId }, cancellationToken);

        if (report == null)
        {
            throw new InvalidOperationException($"Report {reportId.Value} not found");
        }

        if (string.IsNullOrEmpty(report.FilePath))
        {
            throw new InvalidOperationException($"Report {reportId.Value} has no file path");
        }

        return report.FilePath;
    }
}
