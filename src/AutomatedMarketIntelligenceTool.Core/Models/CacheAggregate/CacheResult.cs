namespace AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;

/// <summary>
/// Represents the result of a cache lookup operation.
/// </summary>
/// <typeparam name="T">The type of the cached data.</typeparam>
public class CacheResult<T>
{
    /// <summary>
    /// Whether the value was found in cache (hit) or fetched (miss).
    /// </summary>
    public bool WasHit { get; }

    /// <summary>
    /// The cached or fetched value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The cache key used for the lookup.
    /// </summary>
    public string? CacheKey { get; }

    /// <summary>
    /// When the entry expires (if from cache).
    /// </summary>
    public DateTime? ExpiresAt { get; }

    /// <summary>
    /// Number of times this cache entry has been accessed.
    /// </summary>
    public int HitCount { get; }

    private CacheResult(T value, bool wasHit, string? cacheKey = null, DateTime? expiresAt = null, int hitCount = 0)
    {
        Value = value;
        WasHit = wasHit;
        CacheKey = cacheKey;
        ExpiresAt = expiresAt;
        HitCount = hitCount;
    }

    /// <summary>
    /// Creates a cache hit result.
    /// </summary>
    public static CacheResult<T> Hit(T value, string cacheKey, DateTime expiresAt, int hitCount)
    {
        return new CacheResult<T>(value, wasHit: true, cacheKey, expiresAt, hitCount);
    }

    /// <summary>
    /// Creates a cache miss result (value was fetched).
    /// </summary>
    public static CacheResult<T> Miss(T value, string? cacheKey = null, DateTime? expiresAt = null)
    {
        return new CacheResult<T>(value, wasHit: false, cacheKey, expiresAt);
    }
}

/// <summary>
/// Statistics about cache operations.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cache entries.
    /// </summary>
    public int TotalEntries { get; init; }

    /// <summary>
    /// Total size of all cached entries in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    public long TotalHits { get; init; }

    /// <summary>
    /// Number of entries that have expired.
    /// </summary>
    public int ExpiredEntries { get; init; }

    /// <summary>
    /// Cache hit rate as a percentage (0-100).
    /// </summary>
    public double HitRatePercent { get; init; }

    /// <summary>
    /// Average entry size in bytes.
    /// </summary>
    public long AverageEntrySizeBytes => TotalEntries > 0 ? TotalSizeBytes / TotalEntries : 0;

    /// <summary>
    /// Oldest entry timestamp.
    /// </summary>
    public DateTime? OldestEntry { get; init; }

    /// <summary>
    /// Newest entry timestamp.
    /// </summary>
    public DateTime? NewestEntry { get; init; }
}
