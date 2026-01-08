namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public interface ISiteScraper
{
    string SiteName { get; }

    Task<IEnumerable<ScrapedListing>> ScrapeAsync(
        SearchParameters parameters,
        IProgress<ScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
