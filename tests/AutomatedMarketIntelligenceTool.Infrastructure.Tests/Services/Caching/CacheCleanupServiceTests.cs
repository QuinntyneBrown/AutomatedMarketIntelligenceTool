using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Caching;

public class CacheCleanupServiceTests
{
    private readonly Mock<ILogger<ResponseCacheService>> _cacheLoggerMock;
    private readonly Mock<ILogger<CacheCleanupService>> _cleanupLoggerMock;
    private readonly CacheConfiguration _config;

    public CacheCleanupServiceTests()
    {
        _cacheLoggerMock = new Mock<ILogger<ResponseCacheService>>();
        _cleanupLoggerMock = new Mock<ILogger<CacheCleanupService>>();
        _config = new CacheConfiguration
        {
            Enabled = true,
            CleanupIntervalMinutes = 1,
            DefaultTtlHours = 1
        };
    }

    private ResponseCacheService CreateCacheService()
    {
        var options = Options.Create(_config);
        return new ResponseCacheService(options, _cacheLoggerMock.Object);
    }

    private CacheCleanupService CreateCleanupService(ResponseCacheService cacheService)
    {
        var options = Options.Create(_config);
        return new CacheCleanupService(cacheService, options, _cleanupLoggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsWhenCacheServiceIsNull()
    {
        var options = Options.Create(_config);

        Assert.Throws<ArgumentNullException>(() =>
            new CacheCleanupService(null!, options, _cleanupLoggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        var cacheService = CreateCacheService();
        var options = Options.Create(_config);

        Assert.Throws<ArgumentNullException>(() =>
            new CacheCleanupService(cacheService, options, null!));
    }

    [Fact]
    public async Task ExecuteAsync_CompletesWhenCancelled()
    {
        var cacheService = CreateCacheService();
        var cleanupService = CreateCleanupService(cacheService);
        var cts = new CancellationTokenSource();

        cts.Cancel();

        await cleanupService.StartAsync(cts.Token);
        await cleanupService.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_CleansExpiredEntries()
    {
        var cacheService = CreateCacheService();
        var cleanupService = CreateCleanupService(cacheService);

        // Add some entries
        await cacheService.SetAsync("key1", "value1");
        await cacheService.SetAsync("key2", "value2");

        var stats = await cacheService.GetStatisticsAsync();
        Assert.Equal(2, stats.TotalEntries);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        await cleanupService.StartAsync(cts.Token);
        await cleanupService.StopAsync(cts.Token);
    }
}
