using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scraping;

public class ResourceManagerTests
{
    private readonly Mock<ILogger<ResourceManager>> _loggerMock;
    private readonly ResourceManager _resourceManager;

    public ResourceManagerTests()
    {
        _loggerMock = new Mock<ILogger<ResourceManager>>();
        _resourceManager = new ResourceManager(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ResourceManager(null!));
    }

    [Fact]
    public void GetCurrentMetrics_ReturnsValidMetrics()
    {
        // Act
        var metrics = _resourceManager.GetCurrentMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.CpuUsagePercent >= 0);
        Assert.True(metrics.CpuUsagePercent <= 100);
        Assert.True(metrics.MemoryUsageBytes >= 0);
        Assert.True(metrics.TotalMemoryBytes > 0);
    }

    [Fact]
    public void HasAvailableResources_WhenAtMaxConcurrency_ReturnsFalse()
    {
        // Arrange
        var currentConcurrency = 5;
        var maxConcurrency = 5;

        // Act
        var result = _resourceManager.HasAvailableResources(currentConcurrency, maxConcurrency);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAvailableResources_WhenBelowMaxConcurrency_ReturnsTrue()
    {
        // Arrange
        var currentConcurrency = 2;
        var maxConcurrency = 5;

        // Act
        var result = _resourceManager.HasAvailableResources(currentConcurrency, maxConcurrency);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAvailableResources_WhenAboveMaxConcurrency_ReturnsFalse()
    {
        // Arrange
        var currentConcurrency = 6;
        var maxConcurrency = 5;

        // Act
        var result = _resourceManager.HasAvailableResources(currentConcurrency, maxConcurrency);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetRecommendedConcurrency_WithLowLoad_ReturnsRequestedValue()
    {
        // Arrange
        var requestedConcurrency = 5;

        // Act
        var result = _resourceManager.GetRecommendedConcurrency(requestedConcurrency);

        // Assert
        // Should return the requested value or potentially adjust based on current load
        Assert.True(result > 0);
        Assert.True(result <= requestedConcurrency);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetRecommendedConcurrency_AlwaysReturnsPositiveValue(int requestedConcurrency)
    {
        // Act
        var result = _resourceManager.GetRecommendedConcurrency(requestedConcurrency);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void ResourceMetrics_MemoryUsagePercent_CalculatesCorrectly()
    {
        // Arrange
        var metrics = new ResourceMetrics
        {
            MemoryUsageBytes = 500_000_000, // 500 MB
            TotalMemoryBytes = 1_000_000_000, // 1 GB
            CpuUsagePercent = 50
        };

        // Act
        var percentage = metrics.MemoryUsagePercent;

        // Assert
        Assert.Equal(50, percentage);
    }

    [Fact]
    public void ResourceMetrics_IsUnderHighLoad_WhenCpuHigh_ReturnsTrue()
    {
        // Arrange
        var metrics = new ResourceMetrics
        {
            CpuUsagePercent = 85,
            MemoryUsageBytes = 500_000_000,
            TotalMemoryBytes = 1_000_000_000
        };

        // Act & Assert
        Assert.True(metrics.IsUnderHighLoad);
    }

    [Fact]
    public void ResourceMetrics_IsUnderHighLoad_WhenMemoryHigh_ReturnsTrue()
    {
        // Arrange
        var metrics = new ResourceMetrics
        {
            CpuUsagePercent = 50,
            MemoryUsageBytes = 850_000_000, // 85%
            TotalMemoryBytes = 1_000_000_000
        };

        // Act & Assert
        Assert.True(metrics.IsUnderHighLoad);
    }

    [Fact]
    public void ResourceMetrics_IsUnderHighLoad_WhenBothLow_ReturnsFalse()
    {
        // Arrange
        var metrics = new ResourceMetrics
        {
            CpuUsagePercent = 50,
            MemoryUsageBytes = 500_000_000,
            TotalMemoryBytes = 1_000_000_000
        };

        // Act & Assert
        Assert.False(metrics.IsUnderHighLoad);
    }

    [Fact]
    public void ResourceMetrics_IsUnderModerateLoad_WhenCpuModerate_ReturnsTrue()
    {
        // Arrange
        var metrics = new ResourceMetrics
        {
            CpuUsagePercent = 65,
            MemoryUsageBytes = 500_000_000,
            TotalMemoryBytes = 1_000_000_000
        };

        // Act & Assert
        Assert.True(metrics.IsUnderModerateLoad);
    }

    [Fact]
    public void ResourceMetrics_IsUnderModerateLoad_WhenMemoryModerate_ReturnsTrue()
    {
        // Arrange
        var metrics = new ResourceMetrics
        {
            CpuUsagePercent = 50,
            MemoryUsageBytes = 650_000_000, // 65%
            TotalMemoryBytes = 1_000_000_000
        };

        // Act & Assert
        Assert.True(metrics.IsUnderModerateLoad);
    }
}
