using AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;
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
    private readonly IRateLimiter? _rateLimiter;

    public ScraperFactory(ILoggerFactory loggerFactory, IRateLimiter? rateLimiter = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<ScraperFactory>();
        _rateLimiter = rateLimiter;
    }

    public ISiteScraper CreateScraper(string siteName)
    {
        _logger.LogDebug("Creating scraper for site: {SiteName}", siteName);
        
        ISiteScraper scraper = siteName.ToLowerInvariant() switch
        {
            "autotrader" or "autotrader.ca" => new AutotraderScraper(_loggerFactory.CreateLogger<AutotraderScraper>()),
            "kijiji" or "kijiji.ca" => new KijijiScraper(_loggerFactory.CreateLogger<KijijiScraper>()),
            "cargurus" or "cargurus.ca" => new CarGurusScraper(_loggerFactory.CreateLogger<CarGurusScraper>()),
            _ => throw new ArgumentException($"Unsupported site: {siteName}", nameof(siteName))
        };

        // Apply rate limiting decorator if rate limiter is available
        if (_rateLimiter != null)
        {
            _logger.LogDebug("Applying rate limiting decorator to {SiteName}", siteName);
            scraper = new RateLimitingScraperDecorator(
                scraper,
                _rateLimiter,
                _loggerFactory.CreateLogger<RateLimitingScraperDecorator>());
        }

        _logger.LogInformation("Successfully created scraper for site: {SiteName}", siteName);
        return scraper;
    }

    public IEnumerable<ISiteScraper> CreateAllScrapers()
    {
        _logger.LogInformation("Creating all supported scrapers");
        
        yield return CreateScraper("autotrader");
        yield return CreateScraper("kijiji");
        yield return CreateScraper("cargurus");
    }

    public IEnumerable<string> GetSupportedSites()
    {
        return new[] { "Autotrader.ca", "Kijiji.ca", "CarGurus.ca" };
    }
}
