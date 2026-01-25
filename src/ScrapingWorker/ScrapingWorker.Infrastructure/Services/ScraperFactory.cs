using ScrapingOrchestration.Core.Enums;
using ScrapingWorker.Core.Services;

namespace ScrapingWorker.Infrastructure.Services;

/// <summary>
/// Factory for creating and managing site scrapers.
/// </summary>
public sealed class ScraperFactory : IScraperFactory
{
    private readonly Dictionary<ScrapingSource, ISiteScraper> _scrapers;

    public ScraperFactory(IEnumerable<ISiteScraper> scrapers)
    {
        _scrapers = scrapers.ToDictionary(s => s.Source, s => s);
    }

    /// <inheritdoc />
    public ISiteScraper GetScraper(ScrapingSource source)
    {
        if (!_scrapers.TryGetValue(source, out var scraper))
        {
            throw new NotSupportedException($"No scraper registered for source: {source}");
        }
        return scraper;
    }

    /// <inheritdoc />
    public IReadOnlyList<ISiteScraper> GetAllScrapers()
    {
        return _scrapers.Values.ToList();
    }

    /// <inheritdoc />
    public bool HasScraper(ScrapingSource source)
    {
        return _scrapers.ContainsKey(source);
    }
}
