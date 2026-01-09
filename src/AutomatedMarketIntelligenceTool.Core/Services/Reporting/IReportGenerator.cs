using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Reporting;

public interface IReportGenerator
{
    ReportFormat SupportedFormat { get; }
    Task<string> GenerateReportAsync(ReportData data, string outputPath, CancellationToken cancellationToken = default);
}
