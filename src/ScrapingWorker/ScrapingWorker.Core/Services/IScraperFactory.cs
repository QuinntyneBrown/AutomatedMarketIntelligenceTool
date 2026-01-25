using ScrapingOrchestration.Core.Enums;

namespace ScrapingWorker.Core.Services;

/// <summary>
/// Factory for creating site scrapers.
/// </summary>
public interface IScraperFactory
{
    /// <summary>
    /// Gets a scraper for the specified source.
    /// </summary>
    ISiteScraper GetScraper(ScrapingSource source);

    /// <summary>
    /// Gets all available scrapers.
    /// </summary>
    IReadOnlyList<ISiteScraper> GetAllScrapers();

    /// <summary>
    /// Checks if a scraper is available for the specified source.
    /// </summary>
    bool HasScraper(ScrapingSource source);
}
