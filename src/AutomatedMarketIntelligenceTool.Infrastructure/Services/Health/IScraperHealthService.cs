using AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;

/// <summary>
/// Service for managing scraper health monitoring and metrics.
/// </summary>
public interface IScraperHealthService
{
    /// <summary>
    /// Records a scrape attempt and its outcome.
    /// </summary>
    void RecordAttempt(string siteName, bool success, long responseTimeMs, int listingsFound = 0, string? error = null);

    /// <summary>
    /// Records missing elements detected during scraping.
    /// </summary>
    void RecordMissingElements(string siteName, IEnumerable<string> missingElements);

    /// <summary>
    /// Gets the current health metrics for a specific site.
    /// </summary>
    HealthMetrics GetHealthMetrics(string siteName);

    /// <summary>
    /// Gets health metrics for all monitored sites.
    /// </summary>
    IDictionary<string, HealthMetrics> GetAllHealthMetrics();

    /// <summary>
    /// Persists current health metrics to the database.
    /// </summary>
    Task SaveHealthRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical health records for a site.
    /// </summary>
    Task<IEnumerable<ScraperHealthRecord>> GetHealthHistoryAsync(string siteName, int maxRecords = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears in-memory metrics for a site or all sites.
    /// </summary>
    void ClearMetrics(string? siteName = null);
}
