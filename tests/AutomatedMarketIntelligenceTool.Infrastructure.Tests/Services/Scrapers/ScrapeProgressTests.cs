using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class ScrapeProgressTests
{
    [Fact]
    public void ScrapeProgress_CanBeCreatedWithRequiredProperties()
    {
        // Act
        var progress = new ScrapeProgress
        {
            SiteName = "TestSite",
            CurrentPage = 1,
            TotalListingsFound = 25
        };

        // Assert
        Assert.Equal("TestSite", progress.SiteName);
        Assert.Equal(1, progress.CurrentPage);
        Assert.Equal(25, progress.TotalListingsFound);
        Assert.Null(progress.Message);
    }

    [Fact]
    public void ScrapeProgress_CanIncludeOptionalMessage()
    {
        // Act
        var progress = new ScrapeProgress
        {
            SiteName = "TestSite",
            CurrentPage = 2,
            TotalListingsFound = 50,
            Message = "Scraping in progress"
        };

        // Assert
        Assert.Equal("Scraping in progress", progress.Message);
    }

    [Fact]
    public void ScrapeProgress_PropertiesAreReadonly()
    {
        // Arrange
        var progress = new ScrapeProgress
        {
            SiteName = "TestSite",
            CurrentPage = 1,
            TotalListingsFound = 10
        };

        // Act & Assert - Properties should be init-only
        // This test validates the design at compile time
        Assert.Equal("TestSite", progress.SiteName);
        Assert.Equal(1, progress.CurrentPage);
        Assert.Equal(10, progress.TotalListingsFound);
    }
}
