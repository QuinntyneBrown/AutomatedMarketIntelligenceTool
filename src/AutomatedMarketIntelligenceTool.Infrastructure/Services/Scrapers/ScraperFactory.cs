using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public interface IScraperFactory
{
    ISiteScraper CreateScraper(string siteName);
    IEnumerable<ISiteScraper> CreateAllScrapers();
    IEnumerable<string> GetSupportedSites();
}

public class ScraperFactory : IScraperFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ScraperFactory> _logger;

    public ScraperFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<ScraperFactory>();
    }

    public ISiteScraper CreateScraper(string siteName)
    {
        _logger.LogDebug("Creating scraper for site: {SiteName}", siteName);
        
        ISiteScraper scraper = siteName.ToLowerInvariant() switch
        {
            "autotrader" or "autotrader.ca" => new AutotraderScraper(_loggerFactory.CreateLogger<AutotraderScraper>()),
            "kijiji" or "kijiji.ca" => new KijijiScraper(_loggerFactory.CreateLogger<KijijiScraper>()),
            _ => throw new ArgumentException($"Unsupported site: {siteName}", nameof(siteName))
        };

        _logger.LogInformation("Successfully created scraper for site: {SiteName}", siteName);
        return scraper;
    }

    public IEnumerable<ISiteScraper> CreateAllScrapers()
    {
        _logger.LogInformation("Creating all supported scrapers");
        
        yield return new AutotraderScraper(_loggerFactory.CreateLogger<AutotraderScraper>());
        yield return new KijijiScraper(_loggerFactory.CreateLogger<KijijiScraper>());
    }

    public IEnumerable<string> GetSupportedSites()
    {
        return new[] { "Autotrader.ca", "Kijiji.ca" };
    }
}
