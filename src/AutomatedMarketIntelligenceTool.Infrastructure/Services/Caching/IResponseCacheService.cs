using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Caching;

/// <summary>
/// Service for caching HTTP responses from web scraping operations.
/// Implements the cache-aside pattern for efficient data retrieval.
/// </summary>
public interface IResponseCacheService
{
    /// <summary>
    /// Gets a cached value or fetches it using the provided function.
    /// </summary>
    Task<CacheResult<T>> GetOrFetchAsync<T>(
        string key,
        Func<Task<T>> fetchFunc,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached value if it exists.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores a value in the cache.
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries matching the specified pattern.
    /// </summary>
    Task<int> InvalidateAsync(string keyPattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cache entries for a specific URL.
    /// </summary>
    Task InvalidateUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    Task<int> ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a cache key for a URL with optional parameters.
    /// </summary>
    string GenerateCacheKey(string url, string? additionalKey = null);

    /// <summary>
    /// Checks if caching is enabled.
    /// </summary>
    bool IsEnabled { get; }
}
