using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Health;

public class HealthMetricsTests
{
    [Fact]
    public void SuccessRate_WithNoAttempts_ShouldReturnZero()
    {
        // Arrange
        var metrics = new HealthMetrics { SiteName = "TestSite" };

        // Act
        var successRate = metrics.SuccessRate;

        // Assert
        Assert.Equal(0, successRate);
    }

    [Fact]
    public void SuccessRate_WithMixedAttempts_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 10,
            SuccessfulAttempts = 8
        };

        // Act
        var successRate = metrics.SuccessRate;

        // Assert
        Assert.Equal(80, successRate);
    }

    [Fact]
    public void AverageResponseTime_WithNoResponseTimes_ShouldReturnZero()
    {
        // Arrange
        var metrics = new HealthMetrics { SiteName = "TestSite" };

        // Act
        var avgTime = metrics.AverageResponseTime;

        // Assert
        Assert.Equal(0, avgTime);
    }

    [Fact]
    public void AverageResponseTime_WithMultipleResponseTimes_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            ResponseTimes = new List<long> { 100, 200, 300 }
        };

        // Act
        var avgTime = metrics.AverageResponseTime;

        // Assert
        Assert.Equal(200, avgTime);
    }

    [Fact]
    public void HasZeroResults_WhenSuccessfulWithNoListings_ShouldReturnTrue()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            SuccessfulAttempts = 1,
            ListingsFound = 0
        };

        // Act
        var hasZeroResults = metrics.HasZeroResults;

        // Assert
        Assert.True(hasZeroResults);
    }

    [Fact]
    public void HasZeroResults_WhenNoSuccessfulAttempts_ShouldReturnFalse()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            SuccessfulAttempts = 0,
            ListingsFound = 0
        };

        // Act
        var hasZeroResults = metrics.HasZeroResults;

        // Assert
        Assert.False(hasZeroResults);
    }

    [Fact]
    public void GetHealthStatus_WhenAllAttemptsSuccessful_ShouldReturnHealthy()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 10,
            SuccessfulAttempts = 10,
            ListingsFound = 50
        };

        // Act
        var status = metrics.GetHealthStatus();

        // Assert
        Assert.Equal(ScraperHealthStatus.Healthy, status);
    }

    [Fact]
    public void GetHealthStatus_WhenAllAttemptsFailed_ShouldReturnFailed()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 3,
            SuccessfulAttempts = 0,
            FailedAttempts = 3
        };

        // Act
        var status = metrics.GetHealthStatus();

        // Assert
        Assert.Equal(ScraperHealthStatus.Failed, status);
    }

    [Fact]
    public void GetHealthStatus_WhenSuccessRateBelow80_ShouldReturnDegraded()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 10,
            SuccessfulAttempts = 6,  // 60% success rate
            FailedAttempts = 4,
            ListingsFound = 30
        };

        // Act
        var status = metrics.GetHealthStatus();

        // Assert
        Assert.Equal(ScraperHealthStatus.Degraded, status);
    }

    [Fact]
    public void GetHealthStatus_WithMissingElements_ShouldReturnDegraded()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 5,
            SuccessfulAttempts = 5,
            ListingsFound = 25,
            MissingElementCount = 2,
            MissingElements = new List<string> { "price", "title" }
        };

        // Act
        var status = metrics.GetHealthStatus();

        // Assert
        Assert.Equal(ScraperHealthStatus.Degraded, status);
    }

    [Fact]
    public void GetHealthStatus_WithZeroResults_ShouldReturnDegraded()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 3,
            SuccessfulAttempts = 3,
            ListingsFound = 0  // Zero results indicates potential breakage
        };

        // Act
        var status = metrics.GetHealthStatus();

        // Assert
        Assert.Equal(ScraperHealthStatus.Degraded, status);
    }

    [Fact]
    public void GetHealthStatus_WhenSuccessRateBelow20_ShouldReturnFailed()
    {
        // Arrange
        var metrics = new HealthMetrics 
        { 
            SiteName = "TestSite",
            TotalAttempts = 10,
            SuccessfulAttempts = 1,  // 10% success rate
            FailedAttempts = 9
        };

        // Act
        var status = metrics.GetHealthStatus();

        // Assert
        Assert.Equal(ScraperHealthStatus.Failed, status);
    }
}
