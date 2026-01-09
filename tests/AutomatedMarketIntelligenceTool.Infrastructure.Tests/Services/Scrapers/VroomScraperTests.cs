using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class VroomScraperTests
{
    private readonly VroomScraper _scraper;

    public VroomScraperTests()
    {
        _scraper = new VroomScraper(new NullLogger<VroomScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnVroom()
    {
        // Assert
        Assert.Equal("Vroom.com", _scraper.SiteName);
    }
}
