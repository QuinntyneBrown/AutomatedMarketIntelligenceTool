namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;

/// <summary>
/// Represents health metrics for a scraper instance.
/// </summary>
public class HealthMetrics
{
    public required string SiteName { get; init; }
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public int ListingsFound { get; set; }
    public int MissingElementCount { get; set; }
    public List<string> MissingElements { get; set; } = new();
    public List<long> ResponseTimes { get; set; } = new();
    public string? LastError { get; set; }
    public DateTime LastAttemptedAt { get; set; }
    public DateTime LastSuccessAt { get; set; }

    /// <summary>
    /// Calculates the success rate as a percentage.
    /// </summary>
    public decimal SuccessRate => TotalAttempts > 0 
        ? (decimal)SuccessfulAttempts / TotalAttempts * 100 
        : 0;

    /// <summary>
    /// Calculates the average response time in milliseconds.
    /// </summary>
    public long AverageResponseTime => ResponseTimes.Count > 0 
        ? (long)ResponseTimes.Average() 
        : 0;

    /// <summary>
    /// Determines if zero results were found which may indicate scraper breakage.
    /// </summary>
    public bool HasZeroResults => SuccessfulAttempts > 0 && ListingsFound == 0;

    /// <summary>
    /// Determines the current health status based on metrics.
    /// </summary>
    public ScraperHealthStatus GetHealthStatus()
    {
        // Failed if last 3 attempts failed or success rate below 20%
        if (TotalAttempts >= 3 && SuccessfulAttempts == 0)
            return ScraperHealthStatus.Failed;
        
        if (SuccessRate < 20 && TotalAttempts >= 5)
            return ScraperHealthStatus.Failed;

        // Degraded if success rate below 80% or missing elements detected or zero results
        if (SuccessRate < 80 && TotalAttempts >= 3)
            return ScraperHealthStatus.Degraded;
        
        if (MissingElementCount > 0)
            return ScraperHealthStatus.Degraded;
        
        if (HasZeroResults)
            return ScraperHealthStatus.Degraded;

        return ScraperHealthStatus.Healthy;
    }
}
