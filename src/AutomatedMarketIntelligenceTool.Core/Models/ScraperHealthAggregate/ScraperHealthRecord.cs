namespace AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;

/// <summary>
/// Represents a health record for a scraper at a specific point in time.
/// </summary>
public class ScraperHealthRecord
{
    public ScraperHealthRecordId ScraperHealthRecordId { get; private set; }
    public string SiteName { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public decimal SuccessRate { get; private set; }
    public int ListingsFound { get; private set; }
    public int ErrorCount { get; private set; }
    public long AverageResponseTime { get; private set; }
    public string? LastError { get; private set; }
    public string Status { get; private set; }
    public int MissingElementCount { get; private set; }
    public string? MissingElementsJson { get; private set; }

    private ScraperHealthRecord()
    {
        ScraperHealthRecordId = ScraperHealthRecordId.Create();
        SiteName = string.Empty;
        Status = string.Empty;
    }

    public static ScraperHealthRecord Create(
        string siteName,
        decimal successRate,
        int listingsFound,
        int errorCount,
        long averageResponseTime,
        string? lastError,
        string status,
        int missingElementCount = 0,
        string? missingElementsJson = null)
    {
        return new ScraperHealthRecord
        {
            ScraperHealthRecordId = ScraperHealthRecordId.Create(),
            SiteName = siteName,
            RecordedAt = DateTime.UtcNow,
            SuccessRate = successRate,
            ListingsFound = listingsFound,
            ErrorCount = errorCount,
            AverageResponseTime = averageResponseTime,
            LastError = lastError,
            Status = status,
            MissingElementCount = missingElementCount,
            MissingElementsJson = missingElementsJson
        };
    }
}
