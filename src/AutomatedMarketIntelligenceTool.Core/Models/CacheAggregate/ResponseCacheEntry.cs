namespace AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;

/// <summary>
/// Represents a cached response entry for web scraping operations.
/// </summary>
public class ResponseCacheEntry
{
    public ResponseCacheEntryId CacheEntryId { get; private set; }
    public string CacheKey { get; private set; }
    public string Url { get; private set; }
    public byte[] ResponseData { get; private set; }
    public string? ContentType { get; private set; }
    public DateTime CachedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int HitCount { get; private set; }
    public long ResponseSizeBytes { get; private set; }

    private ResponseCacheEntry()
    {
        CacheEntryId = ResponseCacheEntryId.Create();
        CacheKey = string.Empty;
        Url = string.Empty;
        ResponseData = Array.Empty<byte>();
    }

    public static ResponseCacheEntry Create(
        string cacheKey,
        string url,
        byte[] responseData,
        TimeSpan ttl,
        string? contentType = null)
    {
        var now = DateTime.UtcNow;
        return new ResponseCacheEntry
        {
            CacheEntryId = ResponseCacheEntryId.Create(),
            CacheKey = cacheKey,
            Url = url,
            ResponseData = responseData,
            ContentType = contentType,
            CachedAt = now,
            ExpiresAt = now.Add(ttl),
            HitCount = 0,
            ResponseSizeBytes = responseData.Length
        };
    }

    public void IncrementHitCount()
    {
        HitCount++;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public void UpdateExpiration(TimeSpan ttl)
    {
        ExpiresAt = DateTime.UtcNow.Add(ttl);
    }
}
