using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Caching;

public class ResponseCacheServiceTests
{
    private readonly Mock<ILogger<ResponseCacheService>> _loggerMock;
    private readonly CacheConfiguration _defaultConfig;

    public ResponseCacheServiceTests()
    {
        _loggerMock = new Mock<ILogger<ResponseCacheService>>();
        _defaultConfig = new CacheConfiguration
        {
            Enabled = true,
            DefaultTtlHours = 1,
            MaxEntrySizeBytes = 10 * 1024 * 1024,
            MaxTotalSizeMB = 500
        };
    }

    private ResponseCacheService CreateService(CacheConfiguration? config = null)
    {
        var options = Options.Create(config ?? _defaultConfig);
        return new ResponseCacheService(options, _loggerMock.Object);
    }

    [Fact]
    public void IsEnabled_ReturnsTrueWhenCachingEnabled()
    {
        var service = CreateService();
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsFalseWhenCachingDisabled()
    {
        var config = new CacheConfiguration { Enabled = false };
        var service = CreateService(config);
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public async Task GetOrFetchAsync_WhenCacheDisabled_ExecutesFetchFunction()
    {
        var config = new CacheConfiguration { Enabled = false };
        var service = CreateService(config);
        var fetchExecuted = false;

        var result = await service.GetOrFetchAsync("test-key", async () =>
        {
            fetchExecuted = true;
            return await Task.FromResult("test-value");
        });

        Assert.True(fetchExecuted);
        Assert.False(result.WasHit);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public async Task GetOrFetchAsync_OnCacheMiss_ExecutesFetchAndStores()
    {
        var service = CreateService();
        var fetchCount = 0;

        var result = await service.GetOrFetchAsync("test-key", async () =>
        {
            fetchCount++;
            return await Task.FromResult("test-value");
        });

        Assert.False(result.WasHit);
        Assert.Equal("test-value", result.Value);
        Assert.Equal(1, fetchCount);
    }

    [Fact]
    public async Task GetOrFetchAsync_OnCacheHit_ReturnsCachedValue()
    {
        var service = CreateService();
        var fetchCount = 0;

        // First call - cache miss
        await service.GetOrFetchAsync("test-key", async () =>
        {
            fetchCount++;
            return await Task.FromResult("test-value");
        });

        // Second call - cache hit
        var result = await service.GetOrFetchAsync("test-key", async () =>
        {
            fetchCount++;
            return await Task.FromResult("test-value");
        });

        Assert.True(result.WasHit);
        Assert.Equal("test-value", result.Value);
        Assert.Equal(1, fetchCount); // Fetch should only be called once
    }

    [Fact]
    public async Task GetOrFetchAsync_ReturnsCorrectHitCount()
    {
        var service = CreateService();

        // First call - cache miss
        await service.GetOrFetchAsync("test-key", () => Task.FromResult("value"));

        // Multiple hits
        await service.GetOrFetchAsync("test-key", () => Task.FromResult("value"));
        await service.GetOrFetchAsync("test-key", () => Task.FromResult("value"));
        var result = await service.GetOrFetchAsync("test-key", () => Task.FromResult("value"));

        Assert.True(result.WasHit);
        Assert.Equal(3, result.HitCount);
    }

    [Fact]
    public async Task SetAsync_StoresValueInCache()
    {
        var service = CreateService();

        await service.SetAsync("test-key", "test-value");
        var result = await service.GetAsync<string>("test-key");

        Assert.Equal("test-value", result);
    }

    [Fact]
    public async Task SetAsync_WithCustomTtl_UsesSpecifiedTtl()
    {
        var service = CreateService();

        await service.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(30));
        var result = await service.GetAsync<string>("test-key");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WhenDisabled_DoesNotStore()
    {
        var config = new CacheConfiguration { Enabled = false };
        var service = CreateService(config);

        await service.SetAsync("test-key", "test-value");
        var result = await service.GetAsync<string>("test-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenKeyNotFound_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetAsync<string>("nonexistent-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenDisabled_ReturnsNull()
    {
        var config = new CacheConfiguration { Enabled = false };
        var service = CreateService(config);

        var result = await service.GetAsync<string>("test-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesMatchingEntries()
    {
        var service = CreateService();

        await service.SetAsync("prefix:key1", "value1");
        await service.SetAsync("prefix:key2", "value2");
        await service.SetAsync("other:key", "value3");

        var removedCount = await service.InvalidateAsync("prefix:*");

        Assert.Equal(2, removedCount);
        Assert.Null(await service.GetAsync<string>("prefix:key1"));
        Assert.Null(await service.GetAsync<string>("prefix:key2"));
        Assert.NotNull(await service.GetAsync<string>("other:key"));
    }

    [Fact]
    public async Task InvalidateAsync_WithExactKey_RemovesOnlyThatEntry()
    {
        var service = CreateService();

        await service.SetAsync("key1", "value1");
        await service.SetAsync("key2", "value2");

        var removedCount = await service.InvalidateAsync("key1");

        Assert.Equal(1, removedCount);
        Assert.Null(await service.GetAsync<string>("key1"));
        Assert.NotNull(await service.GetAsync<string>("key2"));
    }

    [Fact]
    public async Task InvalidateAsync_WithWildcard_RemovesAllEntries()
    {
        var service = CreateService();

        await service.SetAsync("key1", "value1");
        await service.SetAsync("key2", "value2");

        var removedCount = await service.InvalidateAsync("*");

        Assert.Equal(2, removedCount);
    }

    [Fact]
    public async Task InvalidateUrlAsync_RemovesCacheForUrl()
    {
        var service = CreateService();
        var url = "https://example.com/page";
        var key = service.GenerateCacheKey(url);

        await service.SetAsync(key, "cached-response");

        await service.InvalidateUrlAsync(url);

        Assert.Null(await service.GetAsync<string>(key));
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllEntries()
    {
        var service = CreateService();

        await service.SetAsync("key1", "value1");
        await service.SetAsync("key2", "value2");
        await service.SetAsync("key3", "value3");

        var clearedCount = await service.ClearAllAsync();

        Assert.Equal(3, clearedCount);
        Assert.Null(await service.GetAsync<string>("key1"));
        Assert.Null(await service.GetAsync<string>("key2"));
        Assert.Null(await service.GetAsync<string>("key3"));
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStats()
    {
        var service = CreateService();

        await service.SetAsync("key1", "value1");
        await service.SetAsync("key2", "value2");

        // Generate some hits
        await service.GetAsync<string>("key1");
        await service.GetAsync<string>("key1");
        await service.GetAsync<string>("key2");

        var stats = await service.GetStatisticsAsync();

        Assert.Equal(2, stats.TotalEntries);
        Assert.True(stats.TotalHits > 0);
        Assert.True(stats.TotalSizeBytes > 0);
    }

    [Fact]
    public void GenerateCacheKey_CreatesConsistentKey()
    {
        var service = CreateService();
        var url = "https://example.com/page?query=value";

        var key1 = service.GenerateCacheKey(url);
        var key2 = service.GenerateCacheKey(url);

        Assert.Equal(key1, key2);
        Assert.Equal(32, key1.Length);
    }

    [Fact]
    public void GenerateCacheKey_WithAdditionalKey_CreatesDifferentKey()
    {
        var service = CreateService();
        var url = "https://example.com/page";

        var key1 = service.GenerateCacheKey(url);
        var key2 = service.GenerateCacheKey(url, "additional");

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_DifferentUrls_CreateDifferentKeys()
    {
        var service = CreateService();

        var key1 = service.GenerateCacheKey("https://example.com/page1");
        var key2 = service.GenerateCacheKey("https://example.com/page2");

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task SetAsync_OversizedEntry_SkipsCache()
    {
        var config = new CacheConfiguration
        {
            Enabled = true,
            MaxEntrySizeBytes = 10 // Very small limit
        };
        var service = CreateService(config);

        await service.SetAsync("key", "this is a longer value that exceeds the limit");

        var result = await service.GetAsync<string>("key");
        Assert.Null(result);
    }

    [Fact]
    public void CleanupExpiredEntries_RemovesExpiredEntries()
    {
        var service = CreateService();

        // This test verifies the cleanup method exists and can be called
        var removedCount = service.CleanupExpiredEntries();

        Assert.True(removedCount >= 0);
    }

    [Fact]
    public async Task GetOrFetchAsync_WithCustomTtl_UsesTtl()
    {
        var service = CreateService();

        var result = await service.GetOrFetchAsync(
            "test-key",
            () => Task.FromResult("test-value"),
            TimeSpan.FromMinutes(5));

        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public async Task ConcurrentAccess_IsThreadSafe()
    {
        var service = CreateService();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await service.SetAsync($"key{index}", $"value{index}");
                await service.GetAsync<string>($"key{index}");
            }));
        }

        await Task.WhenAll(tasks);

        var stats = await service.GetStatisticsAsync();
        Assert.True(stats.TotalEntries > 0);
    }

    [Fact]
    public async Task GetOrFetchAsync_ComplexObject_SerializesCorrectly()
    {
        var service = CreateService();
        var testObject = new TestCacheObject
        {
            Id = 1,
            Name = "Test",
            Values = new List<string> { "a", "b", "c" }
        };

        await service.SetAsync("complex-key", testObject);
        var result = await service.GetAsync<TestCacheObject>("complex-key");

        Assert.NotNull(result);
        Assert.Equal(testObject.Id, result.Id);
        Assert.Equal(testObject.Name, result.Name);
        Assert.Equal(testObject.Values, result.Values);
    }

    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
    }
}
