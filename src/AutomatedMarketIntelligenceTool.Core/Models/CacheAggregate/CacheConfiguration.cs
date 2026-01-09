namespace AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;

/// <summary>
/// Configuration settings for the response caching system.
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Whether caching is enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default time-to-live for cached entries in hours. Default: 1 hour.
    /// </summary>
    public int DefaultTtlHours { get; set; } = 1;

    /// <summary>
    /// Maximum size of a single cache entry in bytes. Default: 10MB.
    /// </summary>
    public long MaxEntrySizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum total cache size in megabytes. Default: 500MB.
    /// </summary>
    public int MaxTotalSizeMB { get; set; } = 500;

    /// <summary>
    /// Interval for cache cleanup in minutes. Default: 15 minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Number of days to keep cache statistics. Default: 7 days.
    /// </summary>
    public int StatisticsRetentionDays { get; set; } = 7;

    /// <summary>
    /// Site-specific TTL overrides (site name -> TTL in hours).
    /// </summary>
    public Dictionary<string, int> SiteTtlOverrides { get; set; } = new();

    /// <summary>
    /// Gets the TTL for a specific site.
    /// </summary>
    public TimeSpan GetTtlForSite(string siteName)
    {
        if (SiteTtlOverrides.TryGetValue(siteName, out var hours))
        {
            return TimeSpan.FromHours(hours);
        }
        return TimeSpan.FromHours(DefaultTtlHours);
    }

    /// <summary>
    /// Maximum total cache size in bytes.
    /// </summary>
    public long MaxTotalSizeBytes => MaxTotalSizeMB * 1024L * 1024L;
}
