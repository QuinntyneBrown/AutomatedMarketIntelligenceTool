namespace Notification.Core.Entities;

/// <summary>
/// Represents a notification template for sending formatted messages.
/// </summary>
public sealed class NotificationTemplate
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private NotificationTemplate() { }

    public static NotificationTemplate Create(string name, string subject, string body, string type)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Subject = subject,
            Body = body,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    public void Update(string name, string subject, string body, string type, bool isActive)
    {
        Name = name;
        Subject = subject;
        Body = body;
        Type = type;
        IsActive = isActive;
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
}
