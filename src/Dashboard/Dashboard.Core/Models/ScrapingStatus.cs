namespace Dashboard.Core.Models;

/// <summary>
/// Represents the status of scraping operations.
/// </summary>
public sealed class ScrapingStatus
{
    /// <summary>
    /// Number of currently active scraping jobs.
    /// </summary>
    public int ActiveJobs { get; init; }

    /// <summary>
    /// Number of scraping jobs completed today.
    /// </summary>
    public int CompletedToday { get; init; }

    /// <summary>
    /// Number of scraping jobs that failed today.
    /// </summary>
    public int FailedToday { get; init; }

    /// <summary>
    /// Next scheduled scraping job time.
    /// </summary>
    public DateTimeOffset? NextScheduledJob { get; init; }

    /// <summary>
    /// Total listings scraped today.
    /// </summary>
    public int ListingsScrapedToday { get; init; }

    /// <summary>
    /// Status breakdown by source.
    /// </summary>
    public IReadOnlyList<SourceScrapingStatus> SourceStatuses { get; init; } = [];

    /// <summary>
    /// Last successful scrape time.
    /// </summary>
    public DateTimeOffset? LastSuccessfulScrape { get; init; }

    /// <summary>
    /// Timestamp when this status was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents the scraping status for a specific source.
/// </summary>
public sealed class SourceScrapingStatus
{
    /// <summary>
    /// Name of the source (e.g., AutoTrader, Kijiji).
    /// </summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>
    /// Whether the scraper for this source is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Last scrape time for this source.
    /// </summary>
    public DateTimeOffset? LastScrapeTime { get; init; }

    /// <summary>
    /// Number of listings from this source.
    /// </summary>
    public int ListingCount { get; init; }

    /// <summary>
    /// Success rate for this source (percentage).
    /// </summary>
    public decimal SuccessRate { get; init; }
}
