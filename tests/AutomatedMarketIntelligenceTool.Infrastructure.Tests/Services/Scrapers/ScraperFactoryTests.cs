using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class ScraperFactoryTests
{
    private readonly ILoggerFactory _loggerFactory;

    public ScraperFactoryTests()
    {
        _loggerFactory = new NullLoggerFactory();
    }

    [Fact]
    public void CreateScraper_WithAutotrader_ShouldReturnAutotraderScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("autotrader");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<AutotraderScraper>(scraper);
        Assert.Equal("Autotrader.ca", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithKijiji_ShouldReturnKijijiScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("kijiji");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<KijijiScraper>(scraper);
        Assert.Equal("Kijiji.ca", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithKijijiDotCa_ShouldReturnKijijiScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("kijiji.ca");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<KijijiScraper>(scraper);
    }

    [Fact]
    public void CreateScraper_WithCaseInsensitiveAutotrader_ShouldReturnAutotraderScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("AUTOTRADER");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<AutotraderScraper>(scraper);
    }

    [Fact]
    public void CreateScraper_WithUnsupportedSite_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            factory.CreateScraper("unsupported"));

        Assert.Contains("Unsupported site", exception.Message);
        Assert.Equal("siteName", exception.ParamName);
    }

    [Fact]
    public void CreateAllScrapers_ShouldReturnAllSupportedScrapers()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scrapers = factory.CreateAllScrapers().ToList();

        // Assert
        Assert.NotEmpty(scrapers);
        Assert.Equal(2, scrapers.Count);
        Assert.Contains(scrapers, s => s is AutotraderScraper);
        Assert.Contains(scrapers, s => s is KijijiScraper);
    }

    [Fact]
    public void GetSupportedSites_ShouldReturnAllSiteNames()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var sites = factory.GetSupportedSites().ToList();

        // Assert
        Assert.NotEmpty(sites);
        Assert.Equal(2, sites.Count);
        Assert.Contains("Autotrader.ca", sites);
        Assert.Contains("Kijiji.ca", sites);
    }

    [Fact]
    public void CreateScraper_MultipleCalls_ShouldReturnNewInstances()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper1 = factory.CreateScraper("autotrader");
        var scraper2 = factory.CreateScraper("autotrader");

        // Assert
        Assert.NotSame(scraper1, scraper2);
    }
}
