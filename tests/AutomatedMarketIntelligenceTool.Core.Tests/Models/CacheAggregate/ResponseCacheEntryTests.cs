using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Models.CacheAggregate;

public class ResponseCacheEntryTests
{
    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var ttl = TimeSpan.FromHours(2);

        var entry = ResponseCacheEntry.Create(
            "cache-key",
            "https://example.com",
            data,
            ttl,
            "application/json");

        Assert.Equal("cache-key", entry.CacheKey);
        Assert.Equal("https://example.com", entry.Url);
        Assert.Equal(data, entry.ResponseData);
        Assert.Equal("application/json", entry.ContentType);
        Assert.Equal(5, entry.ResponseSizeBytes);
        Assert.Equal(0, entry.HitCount);
        Assert.True(entry.CachedAt <= DateTime.UtcNow);
        Assert.True(entry.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithoutContentType_SetsNullContentType()
    {
        var entry = ResponseCacheEntry.Create(
            "key",
            "url",
            new byte[] { 1 },
            TimeSpan.FromHours(1));

        Assert.Null(entry.ContentType);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var entry1 = ResponseCacheEntry.Create("key1", "url1", new byte[] { 1 }, TimeSpan.FromHours(1));
        var entry2 = ResponseCacheEntry.Create("key2", "url2", new byte[] { 2 }, TimeSpan.FromHours(1));

        Assert.NotEqual(entry1.CacheEntryId, entry2.CacheEntryId);
    }

    [Fact]
    public void IncrementHitCount_IncrementsCount()
    {
        var entry = ResponseCacheEntry.Create("key", "url", new byte[] { 1 }, TimeSpan.FromHours(1));

        entry.IncrementHitCount();
        entry.IncrementHitCount();
        entry.IncrementHitCount();

        Assert.Equal(3, entry.HitCount);
    }

    [Fact]
    public void IsExpired_ReturnsFalseWhenNotExpired()
    {
        var entry = ResponseCacheEntry.Create("key", "url", new byte[] { 1 }, TimeSpan.FromHours(1));

        Assert.False(entry.IsExpired());
    }

    [Fact]
    public void IsExpired_ReturnsTrueWhenExpired()
    {
        var entry = ResponseCacheEntry.Create("key", "url", new byte[] { 1 }, TimeSpan.FromMilliseconds(1));

        Thread.Sleep(10); // Wait for entry to expire

        Assert.True(entry.IsExpired());
    }

    [Fact]
    public void UpdateExpiration_ExtendsExpiry()
    {
        var entry = ResponseCacheEntry.Create("key", "url", new byte[] { 1 }, TimeSpan.FromHours(1));
        var originalExpiry = entry.ExpiresAt;

        Thread.Sleep(10);
        entry.UpdateExpiration(TimeSpan.FromHours(2));

        Assert.True(entry.ExpiresAt > originalExpiry);
    }

    [Fact]
    public void ResponseSizeBytes_MatchesDataLength()
    {
        var data = new byte[1024];
        var entry = ResponseCacheEntry.Create("key", "url", data, TimeSpan.FromHours(1));

        Assert.Equal(1024, entry.ResponseSizeBytes);
    }
}

public class ResponseCacheEntryIdTests
{
    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var id1 = ResponseCacheEntryId.Create();
        var id2 = ResponseCacheEntryId.Create();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void FromGuid_CreatesIdFromGuid()
    {
        var guid = Guid.NewGuid();
        var id = ResponseCacheEntryId.FromGuid(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Equals_ReturnsTrueForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = ResponseCacheEntryId.FromGuid(guid);
        var id2 = ResponseCacheEntryId.FromGuid(guid);

        Assert.True(id1.Equals(id2));
        Assert.True(id1 == id2);
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentValue()
    {
        var id1 = ResponseCacheEntryId.Create();
        var id2 = ResponseCacheEntryId.Create();

        Assert.False(id1.Equals(id2));
        Assert.True(id1 != id2);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = ResponseCacheEntryId.FromGuid(guid);
        var id2 = ResponseCacheEntryId.FromGuid(guid);

        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var id = ResponseCacheEntryId.FromGuid(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ConvertsToGuid()
    {
        var id = ResponseCacheEntryId.Create();
        Guid guid = id;

        Assert.Equal(id.Value, guid);
    }

    [Fact]
    public void Equals_WithObject_ReturnsTrueForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = ResponseCacheEntryId.FromGuid(guid);
        object id2 = ResponseCacheEntryId.FromGuid(guid);

        Assert.True(id1.Equals(id2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var id = ResponseCacheEntryId.Create();

        Assert.False(id.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var id = ResponseCacheEntryId.Create();

        Assert.False(id.Equals("not an id"));
    }
}
