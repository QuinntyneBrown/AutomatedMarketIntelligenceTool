using AutomatedMarketIntelligenceTool.Infrastructure;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Health;

public class ScraperHealthServiceTests
{
    private readonly Mock<ILogger<ScraperHealthService>> _loggerMock;

    public ScraperHealthServiceTests()
    {
        _loggerMock = new Mock<ILogger<ScraperHealthService>>();
    }

    private static DbContextOptions<AutomatedMarketIntelligenceToolContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public void RecordAttempt_SuccessfulScrape_ShouldUpdateMetrics()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);

        // Act
        service.RecordAttempt("TestSite", success: true, responseTimeMs: 1000, listingsFound: 25);

        // Assert
        var metrics = service.GetHealthMetrics("TestSite");
        Assert.Equal(1, metrics.TotalAttempts);
        Assert.Equal(1, metrics.SuccessfulAttempts);
        Assert.Equal(0, metrics.FailedAttempts);
        Assert.Equal(25, metrics.ListingsFound);
        Assert.Single(metrics.ResponseTimes);
        Assert.Equal(1000, metrics.ResponseTimes[0]);
    }

    [Fact]
    public void RecordAttempt_FailedScrape_ShouldUpdateMetricsWithError()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);
        var errorMessage = "Connection timeout";

        // Act
        service.RecordAttempt("TestSite", success: false, responseTimeMs: 5000, error: errorMessage);

        // Assert
        var metrics = service.GetHealthMetrics("TestSite");
        Assert.Equal(1, metrics.TotalAttempts);
        Assert.Equal(0, metrics.SuccessfulAttempts);
        Assert.Equal(1, metrics.FailedAttempts);
        Assert.Equal(errorMessage, metrics.LastError);
    }

    [Fact]
    public void RecordAttempt_ZeroResults_ShouldLogWarning()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);

        // Act
        service.RecordAttempt("TestSite", success: true, responseTimeMs: 1000, listingsFound: 0);

        // Assert
        var metrics = service.GetHealthMetrics("TestSite");
        Assert.True(metrics.HasZeroResults);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Zero results")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordAttempt_MultipleAttempts_ShouldAccumulateMetrics()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);

        // Act
        service.RecordAttempt("TestSite", success: true, responseTimeMs: 1000, listingsFound: 10);
        service.RecordAttempt("TestSite", success: true, responseTimeMs: 1500, listingsFound: 15);
        service.RecordAttempt("TestSite", success: false, responseTimeMs: 3000);

        // Assert
        var metrics = service.GetHealthMetrics("TestSite");
        Assert.Equal(3, metrics.TotalAttempts);
        Assert.Equal(2, metrics.SuccessfulAttempts);
        Assert.Equal(1, metrics.FailedAttempts);
        Assert.Equal(25, metrics.ListingsFound);
        Assert.Equal(3, metrics.ResponseTimes.Count);
    }

    [Fact]
    public void RecordMissingElements_ShouldAddToMetrics()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);
        var missingElements = new[] { "price", "title", "mileage" };

        // Act
        service.RecordMissingElements("TestSite", missingElements);

        // Assert
        var metrics = service.GetHealthMetrics("TestSite");
        Assert.Equal(3, metrics.MissingElementCount);
        Assert.Equal(3, metrics.MissingElements.Count);
        Assert.Contains("price", metrics.MissingElements);
        Assert.Contains("title", metrics.MissingElements);
        Assert.Contains("mileage", metrics.MissingElements);
    }

    [Fact]
    public void RecordMissingElements_DuplicateElements_ShouldOnlyCountOnce()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);

        // Act
        service.RecordMissingElements("TestSite", new[] { "price", "title" });
        service.RecordMissingElements("TestSite", new[] { "price", "mileage" });

        // Assert
        var metrics = service.GetHealthMetrics("TestSite");
        Assert.Equal(3, metrics.MissingElementCount); // price, title, mileage
        Assert.Contains("price", metrics.MissingElements);
        Assert.Contains("title", metrics.MissingElements);
        Assert.Contains("mileage", metrics.MissingElements);
    }

    [Fact]
    public void GetAllHealthMetrics_ShouldReturnAllSites()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);

        // Act
        service.RecordAttempt("Site1", success: true, responseTimeMs: 1000, listingsFound: 10);
        service.RecordAttempt("Site2", success: true, responseTimeMs: 1500, listingsFound: 20);
        service.RecordAttempt("Site3", success: false, responseTimeMs: 3000);

        // Assert
        var allMetrics = service.GetAllHealthMetrics();
        Assert.Equal(3, allMetrics.Count);
        Assert.Contains("Site1", allMetrics.Keys);
        Assert.Contains("Site2", allMetrics.Keys);
        Assert.Contains("Site3", allMetrics.Keys);
    }

    [Fact]
    public async Task SaveHealthRecordsAsync_ShouldPersistToDatabaseAsync()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);
        
        service.RecordAttempt("TestSite", success: true, responseTimeMs: 1000, listingsFound: 10);

        // Act
        await service.SaveHealthRecordsAsync();

        // Assert
        var savedRecords = await context.ScraperHealthRecords.ToListAsync();
        Assert.Single(savedRecords);
        Assert.Equal("TestSite", savedRecords[0].SiteName);
        Assert.Equal(100m, savedRecords[0].SuccessRate);
        Assert.Equal(10, savedRecords[0].ListingsFound);
    }

    [Fact]
    public void ClearMetrics_ForSpecificSite_ShouldClearOnlyThatSite()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);
        
        service.RecordAttempt("Site1", success: true, responseTimeMs: 1000, listingsFound: 10);
        service.RecordAttempt("Site2", success: true, responseTimeMs: 1500, listingsFound: 20);

        // Act
        service.ClearMetrics("Site1");

        // Assert
        var allMetrics = service.GetAllHealthMetrics();
        Assert.Single(allMetrics);
        Assert.Contains("Site2", allMetrics.Keys);
        Assert.DoesNotContain("Site1", allMetrics.Keys);
    }

    [Fact]
    public void ClearMetrics_WithoutSiteName_ShouldClearAllMetrics()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);
        var service = new ScraperHealthService(context, _loggerMock.Object);
        
        service.RecordAttempt("Site1", success: true, responseTimeMs: 1000, listingsFound: 10);
        service.RecordAttempt("Site2", success: true, responseTimeMs: 1500, listingsFound: 20);

        // Act
        service.ClearMetrics();

        // Assert
        var allMetrics = service.GetAllHealthMetrics();
        Assert.Empty(allMetrics);
    }
}
