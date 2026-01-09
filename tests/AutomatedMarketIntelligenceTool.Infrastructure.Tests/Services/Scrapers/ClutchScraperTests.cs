using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class ClutchScraperTests
{
    private readonly ClutchScraper _scraper;

    public ClutchScraperTests()
    {
        _scraper = new ClutchScraper(new NullLogger<ClutchScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnClutch()
    {
        // Assert
        Assert.Equal("Clutch.ca", _scraper.SiteName);
    }
}
