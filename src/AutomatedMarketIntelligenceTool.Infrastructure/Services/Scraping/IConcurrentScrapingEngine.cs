using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Service for executing multiple scrapers concurrently with resource management.
/// </summary>
public interface IConcurrentScrapingEngine
{
    /// <summary>
    /// Scrapes multiple sites concurrently.
    /// </summary>
    /// <param name="scrapers">The list of scrapers to execute.</param>
    /// <param name="parameters">The search parameters.</param>
    /// <param name="concurrencyLevel">Maximum number of concurrent scrapers (default: 3).</param>
    /// <param name="progress">Optional progress reporting callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results from all scrapers with site names.</returns>
    Task<IEnumerable<ConcurrentScrapeResult>> ScrapeAsync(
        IEnumerable<ISiteScraper> scrapers,
        SearchParameters parameters,
        int concurrencyLevel = 3,
        IProgress<ConcurrentScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
