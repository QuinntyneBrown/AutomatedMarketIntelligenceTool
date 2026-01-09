using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class CarFaxScraperTests
{
    private readonly CarFaxScraper _scraper;

    public CarFaxScraperTests()
    {
        _scraper = new CarFaxScraper(new NullLogger<CarFaxScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnCarFax()
    {
        // Assert
        Assert.Equal("CarFax.ca", _scraper.SiteName);
    }
}
