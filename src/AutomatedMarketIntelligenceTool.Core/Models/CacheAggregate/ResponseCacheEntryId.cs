namespace AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;

/// <summary>
/// Strongly-typed identifier for ResponseCacheEntry.
/// </summary>
public readonly struct ResponseCacheEntryId : IEquatable<ResponseCacheEntryId>
{
    public Guid Value { get; }

    public ResponseCacheEntryId(Guid value)
    {
        Value = value;
    }

    public static ResponseCacheEntryId Create() => new(Guid.NewGuid());

    public static ResponseCacheEntryId FromGuid(Guid value) => new(value);

    public bool Equals(ResponseCacheEntryId other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is ResponseCacheEntryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(ResponseCacheEntryId left, ResponseCacheEntryId right) => left.Equals(right);

    public static bool operator !=(ResponseCacheEntryId left, ResponseCacheEntryId right) => !left.Equals(right);

    public static implicit operator Guid(ResponseCacheEntryId id) => id.Value;
}
