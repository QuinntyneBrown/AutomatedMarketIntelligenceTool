using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingWorker.Core.Models;

namespace ScrapingWorker.Core.Services;

/// <summary>
/// Interface for site-specific scrapers.
/// </summary>
public interface ISiteScraper
{
    /// <summary>
    /// Gets the source this scraper handles.
    /// </summary>
    ScrapingSource Source { get; }

    /// <summary>
    /// Scrapes listings based on the search parameters.
    /// </summary>
    Task<ScrapeResult> ScrapeAsync(
        SearchParameters parameters,
        IProgress<ScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the scraper is healthy and can connect to the source.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
