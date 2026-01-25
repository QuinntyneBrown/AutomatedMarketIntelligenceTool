namespace Notification.Core.Entities;

/// <summary>
/// Represents a webhook configuration for external notifications.
/// </summary>
public sealed class Webhook
{
    public Guid Id { get; init; }
    public string Url { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string Events { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public int FailureCount { get; private set; }
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    private Webhook() { }

    public static Webhook Create(
        string url,
        string secret,
        string events,
        string? name = null,
        string? description = null)
    {
        return new Webhook
        {
            Id = Guid.NewGuid(),
            Url = url,
            Secret = secret,
            Events = events,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            FailureCount = 0
        };
    }

    public void Update(string url, string events, bool isActive, string? name = null, string? description = null)
    {
        Url = url;
        Events = events;
        IsActive = isActive;
        Name = name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSuccess()
    {
        LastTriggeredAt = DateTimeOffset.UtcNow;
        FailureCount = 0;
    }

    public void RecordFailure()
    {
        LastTriggeredAt = DateTimeOffset.UtcNow;
        FailureCount++;
    }
}
