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
    public void CreateScraper_WithCarGurus_ShouldReturnCarGurusScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("cargurus");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<CarGurusScraper>(scraper);
        Assert.Equal("CarGurus", scraper.SiteName);
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
    public void CreateScraper_WithClutch_ShouldReturnClutchScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("clutch");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<ClutchScraper>(scraper);
        Assert.Equal("Clutch.ca", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithAuto123_ShouldReturnAuto123Scraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("auto123");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<Auto123Scraper>(scraper);
        Assert.Equal("Auto123.com", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithCarMax_ShouldReturnCarMaxScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("carmax");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<CarMaxScraper>(scraper);
        Assert.Equal("CarMax.com", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithCarvana_ShouldReturnCarvanaScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("carvana");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<CarvanaScraper>(scraper);
        Assert.Equal("Carvana.com", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithVroom_ShouldReturnVroomScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("vroom");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<VroomScraper>(scraper);
        Assert.Equal("Vroom.com", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithTrueCar_ShouldReturnTrueCarScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("truecar");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<TrueCarScraper>(scraper);
        Assert.Equal("TrueCar.com", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithCarFax_ShouldReturnCarFaxScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("carfax");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<CarFaxScraper>(scraper);
        Assert.Equal("CarFax.ca", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithTabangiMotors_ShouldReturnTabangiMotorsScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("tabangimotors");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<TabangiMotorsScraper>(scraper);
        Assert.Equal("TabangiMotors.com", scraper.SiteName);
    }

    [Fact]
    public void CreateScraper_WithTabangiMotorsDotCom_ShouldReturnTabangiMotorsScraper()
    {
        // Arrange
        var factory = new ScraperFactory(_loggerFactory);

        // Act
        var scraper = factory.CreateScraper("tabangimotors.com");

        // Assert
        Assert.NotNull(scraper);
        Assert.IsType<TabangiMotorsScraper>(scraper);
        Assert.Equal("TabangiMotors.com", scraper.SiteName);
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
        Assert.Equal(11, scrapers.Count);
        Assert.Contains(scrapers, s => s is AutotraderScraper);
        Assert.Contains(scrapers, s => s is KijijiScraper);
        Assert.Contains(scrapers, s => s is CarGurusScraper);
        Assert.Contains(scrapers, s => s is ClutchScraper);
        Assert.Contains(scrapers, s => s is Auto123Scraper);
        Assert.Contains(scrapers, s => s is CarMaxScraper);
        Assert.Contains(scrapers, s => s is CarvanaScraper);
        Assert.Contains(scrapers, s => s is VroomScraper);
        Assert.Contains(scrapers, s => s is TrueCarScraper);
        Assert.Contains(scrapers, s => s is CarFaxScraper);
        Assert.Contains(scrapers, s => s is TabangiMotorsScraper);
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
        Assert.Equal(11, sites.Count);
        Assert.Contains("Autotrader.ca", sites);
        Assert.Contains("Kijiji.ca", sites);
        Assert.Contains("CarGurus.ca", sites);
        Assert.Contains("Clutch.ca", sites);
        Assert.Contains("Auto123.com", sites);
        Assert.Contains("CarMax.com", sites);
        Assert.Contains("Carvana.com", sites);
        Assert.Contains("Vroom.com", sites);
        Assert.Contains("TrueCar.com", sites);
        Assert.Contains("CarFax.ca", sites);
        Assert.Contains("TabangiMotors.com", sites);
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
