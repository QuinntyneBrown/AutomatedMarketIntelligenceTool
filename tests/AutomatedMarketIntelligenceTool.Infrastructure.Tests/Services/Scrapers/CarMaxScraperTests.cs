using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class CarMaxScraperTests
{
    private readonly CarMaxScraper _scraper;

    public CarMaxScraperTests()
    {
        _scraper = new CarMaxScraper(new NullLogger<CarMaxScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnCarMax()
    {
        // Assert
        Assert.Equal("CarMax.com", _scraper.SiteName);
    }
}
