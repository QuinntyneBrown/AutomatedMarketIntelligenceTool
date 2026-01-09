using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Models.CacheAggregate;

public class CacheConfigurationTests
{
    [Fact]
    public void DefaultConfiguration_HasCorrectDefaults()
    {
        var config = new CacheConfiguration();

        Assert.True(config.Enabled);
        Assert.Equal(1, config.DefaultTtlHours);
        Assert.Equal(10 * 1024 * 1024, config.MaxEntrySizeBytes);
        Assert.Equal(500, config.MaxTotalSizeMB);
        Assert.Equal(15, config.CleanupIntervalMinutes);
        Assert.Equal(7, config.StatisticsRetentionDays);
    }

    [Fact]
    public void GetTtlForSite_ReturnsDefaultWhenNoOverride()
    {
        var config = new CacheConfiguration
        {
            DefaultTtlHours = 2
        };

        var ttl = config.GetTtlForSite("SomeSite");

        Assert.Equal(TimeSpan.FromHours(2), ttl);
    }

    [Fact]
    public void GetTtlForSite_ReturnsOverrideWhenConfigured()
    {
        var config = new CacheConfiguration
        {
            DefaultTtlHours = 2,
            SiteTtlOverrides = new Dictionary<string, int>
            {
                { "CarGurus", 4 },
                { "AutoTrader", 3 }
            }
        };

        var ttl = config.GetTtlForSite("CarGurus");

        Assert.Equal(TimeSpan.FromHours(4), ttl);
    }

    [Fact]
    public void GetTtlForSite_FallsBackToDefaultForUnknownSite()
    {
        var config = new CacheConfiguration
        {
            DefaultTtlHours = 2,
            SiteTtlOverrides = new Dictionary<string, int>
            {
                { "CarGurus", 4 }
            }
        };

        var ttl = config.GetTtlForSite("UnknownSite");

        Assert.Equal(TimeSpan.FromHours(2), ttl);
    }

    [Fact]
    public void MaxTotalSizeBytes_CalculatesCorrectly()
    {
        var config = new CacheConfiguration
        {
            MaxTotalSizeMB = 100
        };

        Assert.Equal(100L * 1024 * 1024, config.MaxTotalSizeBytes);
    }

    [Fact]
    public void MaxTotalSizeBytes_HandlesLargeValues()
    {
        var config = new CacheConfiguration
        {
            MaxTotalSizeMB = 2000
        };

        Assert.Equal(2000L * 1024 * 1024, config.MaxTotalSizeBytes);
    }
}
