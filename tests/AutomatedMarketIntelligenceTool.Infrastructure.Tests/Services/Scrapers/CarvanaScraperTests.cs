using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class CarvanaScraperTests
{
    private readonly CarvanaScraper _scraper;

    public CarvanaScraperTests()
    {
        _scraper = new CarvanaScraper(new NullLogger<CarvanaScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnCarvana()
    {
        // Assert
        Assert.Equal("Carvana.com", _scraper.SiteName);
    }
}
