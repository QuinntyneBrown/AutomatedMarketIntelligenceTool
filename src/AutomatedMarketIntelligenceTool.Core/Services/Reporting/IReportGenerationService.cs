using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Reporting;

public interface IReportGenerationService
{
    Task<Report> GenerateReportAsync(
        Guid tenantId,
        string name,
        ReportFormat format,
        ReportData data,
        string outputPath,
        string? searchCriteriaJson = null,
        CancellationToken cancellationToken = default);

    Task<string> GetReportPathAsync(ReportId reportId, CancellationToken cancellationToken = default);
}
