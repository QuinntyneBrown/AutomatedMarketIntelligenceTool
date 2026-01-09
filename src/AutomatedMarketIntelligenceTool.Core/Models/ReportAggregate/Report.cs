namespace AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;

public class Report
{
    public ReportId ReportId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public ReportFormat Format { get; private set; }
    public string? SearchCriteriaJson { get; private set; }
    public string? FilePath { get; private set; }
    public long? FileSize { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Report() { }

    public static Report Create(
        Guid tenantId,
        string name,
        ReportFormat format,
        string? searchCriteriaJson = null)
    {
        return new Report
        {
            ReportId = ReportId.CreateNew(),
            TenantId = tenantId,
            Name = name,
            Format = format,
            SearchCriteriaJson = searchCriteriaJson,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsGenerating()
    {
        Status = ReportStatus.Generating;
    }

    public void MarkAsComplete(string filePath, long fileSize)
    {
        Status = ReportStatus.Complete;
        FilePath = filePath;
        FileSize = fileSize;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ReportStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
