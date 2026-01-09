using AutomatedMarketIntelligenceTool.Core.Models.ScrapedListingAggregate;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class RateLimitingScraperDecoratorTests
{
    private readonly Mock<ISiteScraper> _mockScraper;
    private readonly Mock<IRateLimiter> _mockRateLimiter;
    private readonly RateLimitingScraperDecorator _decorator;

    public RateLimitingScraperDecoratorTests()
    {
        _mockScraper = new Mock<ISiteScraper>();
        _mockRateLimiter = new Mock<IRateLimiter>();
        
        _mockScraper.Setup(s => s.SiteName).Returns("TestSite");
        
        _decorator = new RateLimitingScraperDecorator(
            _mockScraper.Object,
            _mockRateLimiter.Object,
            NullLogger<RateLimitingScraperDecorator>.Instance);
    }

    [Fact]
    public void Constructor_WithNullScraper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RateLimitingScraperDecorator(
                null!,
                _mockRateLimiter.Object,
                NullLogger<RateLimitingScraperDecorator>.Instance));
    }

    [Fact]
    public void Constructor_WithNullRateLimiter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RateLimitingScraperDecorator(
                _mockScraper.Object,
                null!,
                NullLogger<RateLimitingScraperDecorator>.Instance));
    }

    [Fact]
    public void SiteName_ShouldReturnInnerScraperSiteName()
    {
        // Act
        var siteName = _decorator.SiteName;

        // Assert
        Assert.Equal("TestSite", siteName);
    }

    [Fact]
    public async Task ScrapeAsync_ShouldCallRateLimiterWaitAsync()
    {
        // Arrange
        var parameters = new SearchParameters { Make = "Toyota" };
        var expectedListings = new List<ScrapedListing>();
        
        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<SearchParameters>(), null, default))
            .ReturnsAsync(expectedListings);

        // Act
        await _decorator.ScrapeAsync(parameters);

        // Assert
        _mockRateLimiter.Verify(
            r => r.WaitAsync(It.IsAny<string>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ScrapeAsync_ShouldCallInnerScraperAfterRateLimiting()
    {
        // Arrange
        var parameters = new SearchParameters { Make = "Toyota" };
        var expectedListings = new List<ScrapedListing>
        {
            new ScrapedListing
            {
                ExternalId = "123",
                SourceSite = "TestSite",
                ListingUrl = "http://test.com/123",
                Make = "Toyota",
                Model = "Camry",
                Year = 2023,
                Price = 25000,
                Condition = Core.Models.ListingAggregate.Enums.Condition.Used
            }
        };
        
        _mockScraper
            .Setup(s => s.ScrapeAsync(parameters, null, default))
            .ReturnsAsync(expectedListings);

        // Act
        var result = await _decorator.ScrapeAsync(parameters);

        // Assert
        _mockScraper.Verify(
            s => s.ScrapeAsync(parameters, null, default),
            Times.Once);
        Assert.Equal(expectedListings, result);
    }

    [Fact]
    public async Task ScrapeAsync_WhenRateLimitExceptionOccurs_ShouldReportAndRetry()
    {
        // Arrange
        var parameters = new SearchParameters { Make = "Toyota" };
        var expectedListings = new List<ScrapedListing>();
        
        _mockScraper
            .SetupSequence(s => s.ScrapeAsync(It.IsAny<SearchParameters>(), null, default))
            .ThrowsAsync(new Exception("HTTP 429 Too Many Requests"))
            .ReturnsAsync(expectedListings);

        // Act
        var result = await _decorator.ScrapeAsync(parameters);

        // Assert
        _mockRateLimiter.Verify(
            r => r.ReportRateLimitHit(It.IsAny<string>()),
            Times.Once);
        _mockRateLimiter.Verify(
            r => r.WaitAsync(It.IsAny<string>(), default),
            Times.Exactly(2)); // Once before first attempt, once before retry
        _mockScraper.Verify(
            s => s.ScrapeAsync(It.IsAny<SearchParameters>(), null, default),
            Times.Exactly(2));
        Assert.Equal(expectedListings, result);
    }

    [Fact]
    public async Task ScrapeAsync_WithProgress_ShouldPassProgressToInnerScraper()
    {
        // Arrange
        var parameters = new SearchParameters { Make = "Toyota" };
        var progress = new Progress<ScrapeProgress>();
        var expectedListings = new List<ScrapedListing>();
        
        _mockScraper
            .Setup(s => s.ScrapeAsync(parameters, progress, default))
            .ReturnsAsync(expectedListings);

        // Act
        await _decorator.ScrapeAsync(parameters, progress);

        // Assert
        _mockScraper.Verify(
            s => s.ScrapeAsync(parameters, progress, default),
            Times.Once);
    }

    [Fact]
    public async Task ScrapeAsync_WithCancellationToken_ShouldPassTokenToRateLimiter()
    {
        // Arrange
        var parameters = new SearchParameters { Make = "Toyota" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedListings = new List<ScrapedListing>();
        
        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<SearchParameters>(), null, token))
            .ReturnsAsync(expectedListings);

        // Act
        await _decorator.ScrapeAsync(parameters, cancellationToken: token);

        // Assert
        _mockRateLimiter.Verify(
            r => r.WaitAsync(It.IsAny<string>(), token),
            Times.Once);
    }

    [Fact]
    public async Task ScrapeAsync_WhenNonRateLimitExceptionOccurs_ShouldNotRetry()
    {
        // Arrange
        var parameters = new SearchParameters { Make = "Toyota" };
        var expectedException = new InvalidOperationException("Some other error");
        
        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<SearchParameters>(), null, default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _decorator.ScrapeAsync(parameters));

        Assert.Equal(expectedException, exception);
        _mockRateLimiter.Verify(
            r => r.ReportRateLimitHit(It.IsAny<string>()),
            Times.Never);
        _mockScraper.Verify(
            s => s.ScrapeAsync(It.IsAny<SearchParameters>(), null, default),
            Times.Once);
    }
}
