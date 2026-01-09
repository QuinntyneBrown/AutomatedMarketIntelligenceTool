namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Progress information for concurrent scraping operations.
/// </summary>
public class ConcurrentScrapeProgress
{
    /// <summary>
    /// Gets the name of the site currently being scraped.
    /// </summary>
    public required string SiteName { get; init; }

    /// <summary>
    /// Gets the total number of sites to scrape.
    /// </summary>
    public required int TotalSites { get; init; }

    /// <summary>
    /// Gets the number of sites completed.
    /// </summary>
    public required int CompletedSites { get; init; }

    /// <summary>
    /// Gets the number of sites currently in progress.
    /// </summary>
    public required int InProgressSites { get; init; }

    /// <summary>
    /// Gets the event type.
    /// </summary>
    public required ConcurrentScrapeEventType EventType { get; init; }

    /// <summary>
    /// Gets optional additional message.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Types of concurrent scrape events.
/// </summary>
public enum ConcurrentScrapeEventType
{
    /// <summary>
    /// A site scrape has started.
    /// </summary>
    Started,

    /// <summary>
    /// A site scrape is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// A site scrape has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// A site scrape has failed.
    /// </summary>
    Failed
}
