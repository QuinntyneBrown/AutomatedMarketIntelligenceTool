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

    public ScraperFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ISiteScraper CreateScraper(string siteName)
    {
        return siteName.ToLowerInvariant() switch
        {
            "autotrader" => new AutotraderScraper(_loggerFactory.CreateLogger<AutotraderScraper>()),
            "cars.com" or "carscom" => new CarsComScraper(_loggerFactory.CreateLogger<CarsComScraper>()),
            _ => throw new ArgumentException($"Unsupported site: {siteName}", nameof(siteName))
        };
    }

    public IEnumerable<ISiteScraper> CreateAllScrapers()
    {
        yield return new AutotraderScraper(_loggerFactory.CreateLogger<AutotraderScraper>());
        yield return new CarsComScraper(_loggerFactory.CreateLogger<CarsComScraper>());
    }

    public IEnumerable<string> GetSupportedSites()
    {
        return new[] { "Autotrader", "Cars.com" };
    }
}
