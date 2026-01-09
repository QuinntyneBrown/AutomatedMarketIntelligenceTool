using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Models.CacheAggregate;

public class CacheResultTests
{
    [Fact]
    public void Hit_CreatesHitResult()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var result = CacheResult<string>.Hit("test-value", "test-key", expiresAt, 5);

        Assert.True(result.WasHit);
        Assert.Equal("test-value", result.Value);
        Assert.Equal("test-key", result.CacheKey);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Equal(5, result.HitCount);
    }

    [Fact]
    public void Miss_CreatesMissResult()
    {
        var result = CacheResult<string>.Miss("test-value", "test-key");

        Assert.False(result.WasHit);
        Assert.Equal("test-value", result.Value);
        Assert.Equal("test-key", result.CacheKey);
        Assert.Null(result.ExpiresAt);
        Assert.Equal(0, result.HitCount);
    }

    [Fact]
    public void Miss_WithoutCacheKey_CreatesMissResult()
    {
        var result = CacheResult<string>.Miss("test-value");

        Assert.False(result.WasHit);
        Assert.Equal("test-value", result.Value);
        Assert.Null(result.CacheKey);
    }

    [Fact]
    public void Hit_WithComplexType_PreservesValue()
    {
        var complexValue = new TestClass { Id = 1, Name = "Test" };
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var result = CacheResult<TestClass>.Hit(complexValue, "key", expiresAt, 0);

        Assert.True(result.WasHit);
        Assert.Same(complexValue, result.Value);
    }

    [Fact]
    public void Miss_WithNullValue_HandleCorrectly()
    {
        var result = CacheResult<string?>.Miss(null);

        Assert.False(result.WasHit);
        Assert.Null(result.Value);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

public class CacheStatisticsTests
{
    [Fact]
    public void AverageEntrySizeBytes_CalculatesCorrectly()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 10,
            TotalSizeBytes = 1000
        };

        Assert.Equal(100, stats.AverageEntrySizeBytes);
    }

    [Fact]
    public void AverageEntrySizeBytes_ReturnsZeroWhenNoEntries()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 0,
            TotalSizeBytes = 0
        };

        Assert.Equal(0, stats.AverageEntrySizeBytes);
    }

    [Fact]
    public void CacheStatistics_InitializesWithDefaultValues()
    {
        var stats = new CacheStatistics();

        Assert.Equal(0, stats.TotalEntries);
        Assert.Equal(0, stats.TotalSizeBytes);
        Assert.Equal(0, stats.TotalHits);
        Assert.Equal(0, stats.ExpiredEntries);
        Assert.Equal(0, stats.HitRatePercent);
    }
}
