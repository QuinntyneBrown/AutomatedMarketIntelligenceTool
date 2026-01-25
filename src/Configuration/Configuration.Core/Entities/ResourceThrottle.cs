namespace Configuration.Core.Entities;

/// <summary>
/// Resource throttling configuration for rate limiting and concurrency control.
/// </summary>
public sealed class ResourceThrottle
{
    public Guid Id { get; init; }
    public string ResourceName { get; private set; } = string.Empty;
    public int MaxConcurrent { get; private set; }
    public int RateLimitPerMinute { get; private set; }
    public bool IsEnabled { get; private set; }

    private ResourceThrottle() { }

    public static ResourceThrottle Create(
        string resourceName,
        int maxConcurrent,
        int rateLimitPerMinute,
        bool isEnabled = true)
    {
        return new ResourceThrottle
        {
            Id = Guid.NewGuid(),
            ResourceName = resourceName,
            MaxConcurrent = maxConcurrent,
            RateLimitPerMinute = rateLimitPerMinute,
            IsEnabled = isEnabled
        };
    }

    public void Update(int? maxConcurrent = null, int? rateLimitPerMinute = null, bool? isEnabled = null)
    {
        if (maxConcurrent.HasValue)
            MaxConcurrent = maxConcurrent.Value;

        if (rateLimitPerMinute.HasValue)
            RateLimitPerMinute = rateLimitPerMinute.Value;

        if (isEnabled.HasValue)
            IsEnabled = isEnabled.Value;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }
}
