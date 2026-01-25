namespace Configuration.Core.Entities;

/// <summary>
/// Feature flag for enabling/disabling features across the system.
/// </summary>
public sealed class FeatureFlag
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private FeatureFlag() { }

    public static FeatureFlag Create(string name, bool isEnabled = false, string? description = null)
    {
        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = isEnabled,
            Description = description,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        if (IsEnabled != enabled)
        {
            IsEnabled = enabled;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
