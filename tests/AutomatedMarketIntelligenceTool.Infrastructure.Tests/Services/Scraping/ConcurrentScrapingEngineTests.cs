using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ScrapedListingAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scraping;

public class ConcurrentScrapingEngineTests
{
    private readonly Mock<IResourceManager> _resourceManagerMock;
    private readonly Mock<ILogger<ConcurrentScrapingEngine>> _loggerMock;
    private readonly ConcurrentScrapingEngine _engine;

    public ConcurrentScrapingEngineTests()
    {
        _resourceManagerMock = new Mock<IResourceManager>();
        _loggerMock = new Mock<ILogger<ConcurrentScrapingEngine>>();
        
        // Default behavior: return requested concurrency
        _resourceManagerMock
            .Setup(x => x.GetRecommendedConcurrency(It.IsAny<int>()))
            .Returns<int>(x => x);

        _engine = new ConcurrentScrapingEngine(
            _resourceManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullResourceManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConcurrentScrapingEngine(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConcurrentScrapingEngine(_resourceManagerMock.Object, null!));
    }

    [Fact]
    public async Task ScrapeAsync_WithEmptyScrapers_ReturnsEmptyResults()
    {
        // Arrange
        var scrapers = Enumerable.Empty<ISiteScraper>();
        var parameters = new SearchParameters();

        // Act
        var results = await _engine.ScrapeAsync(scrapers, parameters);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScrapeAsync_WithSingleScraper_ExecutesSuccessfully()
    {
        // Arrange
        var scraperMock = CreateMockScraper("TestSite", new List<ScrapedListing>
        {
            CreateTestListing("listing1", "TestSite", "Toyota", "Camry", 2020, 25000)
        });

        var scrapers = new[] { scraperMock.Object };
        var parameters = new SearchParameters();

        // Act
        var results = await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: 1);
        var resultsList = results.ToList();

        // Assert
        Assert.Single(resultsList);
        Assert.True(resultsList[0].Success);
        Assert.Equal("TestSite", resultsList[0].SiteName);
        Assert.Single(resultsList[0].Listings);
    }

    [Fact]
    public async Task ScrapeAsync_WithMultipleScrapers_ExecutesAll()
    {
        // Arrange
        var scraper1Mock = CreateMockScraper("Site1", new List<ScrapedListing>
        {
            CreateTestListing("1", "Site1", "Toyota", "Camry", 2020, 25000)
        });

        var scraper2Mock = CreateMockScraper("Site2", new List<ScrapedListing>
        {
            CreateTestListing("2", "Site2", "Honda", "Accord", 2021, 28000)
        });

        var scrapers = new[] { scraper1Mock.Object, scraper2Mock.Object };
        var parameters = new SearchParameters();

        // Act
        var results = await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: 2);
        var resultsList = results.ToList();

        // Assert
        Assert.Equal(2, resultsList.Count);
        Assert.All(resultsList, r => Assert.True(r.Success));
        Assert.Contains(resultsList, r => r.SiteName == "Site1");
        Assert.Contains(resultsList, r => r.SiteName == "Site2");
    }

    [Fact]
    public async Task ScrapeAsync_WhenScraperThrowsException_CapturesError()
    {
        // Arrange
        var scraperMock = new Mock<ISiteScraper>();
        scraperMock.Setup(x => x.SiteName).Returns("FailingSite");
        scraperMock
            .Setup(x => x.ScrapeAsync(It.IsAny<SearchParameters>(), It.IsAny<IProgress<ScrapeProgress>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var scrapers = new[] { scraperMock.Object };
        var parameters = new SearchParameters();

        // Act
        var results = await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: 1);
        var resultsList = results.ToList();

        // Assert
        Assert.Single(resultsList);
        Assert.False(resultsList[0].Success);
        Assert.Equal("FailingSite", resultsList[0].SiteName);
        Assert.Equal("Test exception", resultsList[0].ErrorMessage);
        Assert.NotNull(resultsList[0].Exception);
        Assert.Empty(resultsList[0].Listings);
    }

    [Fact]
    public async Task ScrapeAsync_WithMixedSuccessAndFailure_ReturnsAllResults()
    {
        // Arrange
        var successfulScraper = CreateMockScraper("SuccessSite", new List<ScrapedListing>
        {
            CreateTestListing("1", "SuccessSite", "Toyota", "Camry", 2020, 25000)
        });

        var failingScraper = new Mock<ISiteScraper>();
        failingScraper.Setup(x => x.SiteName).Returns("FailingSite");
        failingScraper
            .Setup(x => x.ScrapeAsync(It.IsAny<SearchParameters>(), It.IsAny<IProgress<ScrapeProgress>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var scrapers = new[] { successfulScraper.Object, failingScraper.Object };
        var parameters = new SearchParameters();

        // Act
        var results = await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: 2);
        var resultsList = results.ToList();

        // Assert
        Assert.Equal(2, resultsList.Count);
        Assert.Single(resultsList, r => r.Success);
        Assert.Single(resultsList, r => !r.Success);
    }

    [Fact]
    public async Task ScrapeAsync_RespectsResourceManagerRecommendation()
    {
        // Arrange
        var requestedConcurrency = 10;
        var recommendedConcurrency = 3;

        _resourceManagerMock
            .Setup(x => x.GetRecommendedConcurrency(It.IsAny<int>()))
            .Returns(recommendedConcurrency);

        var scrapers = new[]
        {
            CreateMockScraper("Site1", new List<ScrapedListing>()).Object,
            CreateMockScraper("Site2", new List<ScrapedListing>()).Object,
            CreateMockScraper("Site3", new List<ScrapedListing>()).Object
        };
        var parameters = new SearchParameters();

        // Act
        await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: requestedConcurrency);

        // Assert
        _resourceManagerMock.Verify(
            x => x.GetRecommendedConcurrency(It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task ScrapeAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var scraper = CreateMockScraper("TestSite", new List<ScrapedListing>
        {
            CreateTestListing("1", "TestSite", "Toyota", "Camry", 2020, 25000)
        });

        var scrapers = new[] { scraper.Object };
        var parameters = new SearchParameters();
        var progressReports = new List<ConcurrentScrapeProgress>();
        var progress = new Progress<ConcurrentScrapeProgress>(p => progressReports.Add(p));

        // Act
        await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: 1, progress: progress);

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.EventType == ConcurrentScrapeEventType.Started);
        Assert.Contains(progressReports, p => p.EventType == ConcurrentScrapeEventType.Completed);
    }

    [Fact]
    public async Task ScrapeAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var scraper = CreateMockScraper("TestSite", new List<ScrapedListing>());
        var scrapers = new[] { scraper.Object };
        var parameters = new SearchParameters();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _engine.ScrapeAsync(scrapers, parameters, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ScrapeAsync_ResultsAreSortedBySiteName()
    {
        // Arrange
        var scrapers = new[]
        {
            CreateMockScraper("ZSite", new List<ScrapedListing>()).Object,
            CreateMockScraper("ASite", new List<ScrapedListing>()).Object,
            CreateMockScraper("MSite", new List<ScrapedListing>()).Object
        };
        var parameters = new SearchParameters();

        // Act
        var results = await _engine.ScrapeAsync(scrapers, parameters, concurrencyLevel: 3);
        var resultsList = results.ToList();

        // Assert
        Assert.Equal("ASite", resultsList[0].SiteName);
        Assert.Equal("MSite", resultsList[1].SiteName);
        Assert.Equal("ZSite", resultsList[2].SiteName);
    }

    private ScrapedListing CreateTestListing(
        string externalId,
        string sourceSite,
        string make,
        string model,
        int year,
        decimal price)
    {
        return new ScrapedListing
        {
            ExternalId = externalId,
            SourceSite = sourceSite,
            Make = make,
            Model = model,
            Year = year,
            Price = price,
            Condition = Condition.Used,
            ListingUrl = $"http://example.com/{externalId}"
        };
    }

    private Mock<ISiteScraper> CreateMockScraper(string siteName, List<ScrapedListing> listings)
    {
        var mock = new Mock<ISiteScraper>();
        mock.Setup(x => x.SiteName).Returns(siteName);
        mock.Setup(x => x.ScrapeAsync(
                It.IsAny<SearchParameters>(),
                It.IsAny<IProgress<ScrapeProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(listings);
        return mock;
    }
}
