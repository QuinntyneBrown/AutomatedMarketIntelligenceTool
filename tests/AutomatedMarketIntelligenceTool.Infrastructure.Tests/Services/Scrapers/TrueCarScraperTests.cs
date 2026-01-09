using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class TrueCarScraperTests
{
    private readonly TrueCarScraper _scraper;

    public TrueCarScraperTests()
    {
        _scraper = new TrueCarScraper(new NullLogger<TrueCarScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnTrueCar()
    {
        // Assert
        Assert.Equal("TrueCar.com", _scraper.SiteName);
    }
}
