using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class Auto123ScraperTests
{
    private readonly Auto123Scraper _scraper;

    public Auto123ScraperTests()
    {
        _scraper = new Auto123Scraper(new NullLogger<Auto123Scraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnAuto123()
    {
        // Assert
        Assert.Equal("Auto123.com", _scraper.SiteName);
    }
}
