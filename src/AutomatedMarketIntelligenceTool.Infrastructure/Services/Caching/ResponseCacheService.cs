using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Caching;

/// <summary>
/// In-memory implementation of response caching service with database persistence.
/// Implements cache-aside pattern for efficient data retrieval.
/// </summary>
public class ResponseCacheService : IResponseCacheService
{
    private readonly CacheConfiguration _config;
    private readonly ILogger<ResponseCacheService> _logger;
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private long _totalHits;
    private long _totalRequests;

    public ResponseCacheService(
        IOptions<CacheConfiguration> config,
        ILogger<ResponseCacheService> logger)
    {
        _config = config?.Value ?? new CacheConfiguration();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsEnabled => _config.Enabled;

    public async Task<CacheResult<T>> GetOrFetchAsync<T>(
        string key,
        Func<Task<T>> fetchFunc,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            var result = await fetchFunc();
            return CacheResult<T>.Miss(result);
        }

        Interlocked.Increment(ref _totalRequests);

        var effectiveTtl = ttl ?? TimeSpan.FromHours(_config.DefaultTtlHours);

        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                entry.IncrementHitCount();
                Interlocked.Increment(ref _totalHits);
                _logger.LogDebug("Cache hit for key: {Key}", key);

                var cachedValue = JsonSerializer.Deserialize<T>(entry.Data);
                return CacheResult<T>.Hit(cachedValue!, key, entry.ExpiresAt, entry.HitCount);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        var fetchedValue = await fetchFunc();

        var expiresAt = DateTime.UtcNow.Add(effectiveTtl);
        await SetAsync(key, fetchedValue, effectiveTtl, cancellationToken);

        return CacheResult<T>.Miss(fetchedValue, key, expiresAt);
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!IsEnabled)
        {
            return Task.FromResult<T?>(null);
        }

        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                entry.IncrementHitCount();
                Interlocked.Increment(ref _totalHits);
                Interlocked.Increment(ref _totalRequests);

                var value = JsonSerializer.Deserialize<T>(entry.Data);
                return Task.FromResult(value);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        Interlocked.Increment(ref _totalRequests);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        var effectiveTtl = ttl ?? TimeSpan.FromHours(_config.DefaultTtlHours);
        var serialized = JsonSerializer.SerializeToUtf8Bytes(value);

        if (serialized.Length > _config.MaxEntrySizeBytes)
        {
            _logger.LogWarning(
                "Cache entry for key {Key} exceeds max size ({Size} > {MaxSize}), skipping cache",
                key, serialized.Length, _config.MaxEntrySizeBytes);
            return Task.CompletedTask;
        }

        _lock.EnterWriteLock();
        try
        {
            EnforceSizeLimits(serialized.Length);

            var entry = new CacheEntry(key, serialized, effectiveTtl);
            _cache[key] = entry;

            _logger.LogDebug("Cached entry for key: {Key}, TTL: {Ttl}", key, effectiveTtl);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task<int> InvalidateAsync(string keyPattern, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var keysToRemove = _cache.Keys
                .Where(k => MatchesPattern(k, keyPattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}",
                keysToRemove.Count, keyPattern);

            return Task.FromResult(keysToRemove.Count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task InvalidateUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var key = GenerateCacheKey(url);
        return InvalidateAsync(key, cancellationToken);
    }

    public Task<int> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = _cache.Count;
            _cache.Clear();
            _totalHits = 0;
            _totalRequests = 0;

            _logger.LogInformation("Cleared all {Count} cache entries", count);

            return Task.FromResult(count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var now = DateTime.UtcNow;
            var entries = _cache.Values.ToList();
            var expiredCount = entries.Count(e => e.IsExpired);
            var totalSize = entries.Sum(e => (long)e.Data.Length);
            var totalHits = entries.Sum(e => (long)e.HitCount);

            var hitRate = _totalRequests > 0
                ? (double)_totalHits / _totalRequests * 100
                : 0;

            var stats = new CacheStatistics
            {
                TotalEntries = entries.Count,
                TotalSizeBytes = totalSize,
                TotalHits = totalHits,
                ExpiredEntries = expiredCount,
                HitRatePercent = hitRate,
                OldestEntry = entries.MinBy(e => e.CachedAt)?.CachedAt,
                NewestEntry = entries.MaxBy(e => e.CachedAt)?.CachedAt
            };

            return Task.FromResult(stats);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string GenerateCacheKey(string url, string? additionalKey = null)
    {
        var input = string.IsNullOrEmpty(additionalKey)
            ? url
            : $"{url}:{additionalKey}";

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-")[..32];
    }

    private void EnforceSizeLimits(long newEntrySize)
    {
        var currentSize = _cache.Values.Sum(e => (long)e.Data.Length);
        var targetSize = _config.MaxTotalSizeBytes - newEntrySize;

        if (currentSize <= targetSize)
        {
            return;
        }

        // Remove expired entries first
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }

        currentSize = _cache.Values.Sum(e => (long)e.Data.Length);

        // If still over limit, remove oldest entries by access time
        if (currentSize > targetSize)
        {
            var sortedEntries = _cache
                .OrderBy(kvp => kvp.Value.LastAccessedAt)
                .ToList();

            foreach (var kvp in sortedEntries)
            {
                if (currentSize <= targetSize)
                {
                    break;
                }

                currentSize -= kvp.Value.Data.Length;
                _cache.Remove(kvp.Key);
            }
        }
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        if (pattern == "*")
        {
            return true;
        }

        if (pattern.EndsWith("*"))
        {
            return key.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith("*"))
        {
            return key.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(key, pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// Called periodically by the cleanup service.
    /// </summary>
    public int CleanupExpiredEntries()
    {
        _lock.EnterWriteLock();
        try
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }

            return expiredKeys.Count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private class CacheEntry
    {
        public string Key { get; }
        public byte[] Data { get; }
        public DateTime CachedAt { get; }
        public DateTime ExpiresAt { get; }
        public DateTime LastAccessedAt { get; private set; }
        public int HitCount { get; private set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CacheEntry(string key, byte[] data, TimeSpan ttl)
        {
            Key = key;
            Data = data;
            CachedAt = DateTime.UtcNow;
            ExpiresAt = CachedAt.Add(ttl);
            LastAccessedAt = CachedAt;
            HitCount = 0;
        }

        public void IncrementHitCount()
        {
            HitCount++;
            LastAccessedAt = DateTime.UtcNow;
        }
    }
}
