namespace Configuration.Core.Entities;

/// <summary>
/// Configuration settings for a specific service.
/// </summary>
public sealed class ServiceConfiguration
{
    public Guid Id { get; init; }
    public string ServiceName { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ServiceConfiguration() { }

    public static ServiceConfiguration Create(string serviceName, string key, string value)
    {
        return new ServiceConfiguration
        {
            Id = Guid.NewGuid(),
            ServiceName = serviceName,
            Key = key,
            Value = value,
            Version = 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateValue(string newValue)
    {
        if (Value != newValue)
        {
            Value = newValue;
            Version++;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
