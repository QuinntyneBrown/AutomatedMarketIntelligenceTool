using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Result from a concurrent scrape operation for a single site.
/// </summary>
public class ConcurrentScrapeResult
{
    /// <summary>
    /// Gets the name of the site that was scraped.
    /// </summary>
    public required string SiteName { get; init; }

    /// <summary>
    /// Gets the listings scraped from this site.
    /// </summary>
    public required IEnumerable<ScrapedListing> Listings { get; init; }

    /// <summary>
    /// Gets a value indicating whether the scrape was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the scrape failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception that occurred if the scrape failed.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the elapsed time for the scrape operation.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }
}
