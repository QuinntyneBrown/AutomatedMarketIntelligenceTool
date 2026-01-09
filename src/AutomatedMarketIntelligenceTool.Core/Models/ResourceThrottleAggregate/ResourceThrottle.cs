namespace AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;

/// <summary>
/// Represents a resource throttle configuration for managing system resource consumption.
/// </summary>
public class ResourceThrottle
{
    public ResourceThrottleId ResourceThrottleId { get; private set; } = null!;
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The type of resource being throttled.
    /// </summary>
    public ResourceType ResourceType { get; private set; }

    /// <summary>
    /// The maximum allowed value for the resource.
    /// </summary>
    public int MaxValue { get; private set; }

    /// <summary>
    /// The time window for the throttle limit.
    /// </summary>
    public ThrottleTimeWindow TimeWindow { get; private set; }

    /// <summary>
    /// Current usage count within the current time window.
    /// </summary>
    public int CurrentUsage { get; private set; }

    /// <summary>
    /// The start of the current time window.
    /// </summary>
    public DateTime WindowStartTime { get; private set; }

    /// <summary>
    /// Whether this throttle is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// The action to take when the limit is reached.
    /// </summary>
    public ThrottleAction Action { get; private set; }

    /// <summary>
    /// Warning threshold percentage (0-100). Logs warning when usage reaches this percentage.
    /// </summary>
    public int WarningThresholdPercent { get; private set; }

    /// <summary>
    /// Optional description of this throttle configuration.
    /// </summary>
    public string? Description { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private ResourceThrottle() { }

    public static ResourceThrottle Create(
        Guid tenantId,
        ResourceType resourceType,
        int maxValue,
        ThrottleTimeWindow timeWindow,
        ThrottleAction action = ThrottleAction.Reject,
        int warningThresholdPercent = 80,
        string? description = null,
        string? createdBy = null)
    {
        if (maxValue <= 0)
            throw new ArgumentException("Max value must be greater than 0", nameof(maxValue));

        if (warningThresholdPercent < 0 || warningThresholdPercent > 100)
            throw new ArgumentException("Warning threshold must be between 0 and 100", nameof(warningThresholdPercent));

        return new ResourceThrottle
        {
            ResourceThrottleId = ResourceThrottleId.CreateNew(),
            TenantId = tenantId,
            ResourceType = resourceType,
            MaxValue = maxValue,
            TimeWindow = timeWindow,
            CurrentUsage = 0,
            WindowStartTime = DateTime.UtcNow,
            IsEnabled = true,
            Action = action,
            WarningThresholdPercent = warningThresholdPercent,
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(
        int maxValue,
        ThrottleTimeWindow timeWindow,
        ThrottleAction action,
        int warningThresholdPercent,
        string? description = null,
        string? updatedBy = null)
    {
        if (maxValue <= 0)
            throw new ArgumentException("Max value must be greater than 0", nameof(maxValue));

        if (warningThresholdPercent < 0 || warningThresholdPercent > 100)
            throw new ArgumentException("Warning threshold must be between 0 and 100", nameof(warningThresholdPercent));

        MaxValue = maxValue;
        TimeWindow = timeWindow;
        Action = action;
        WarningThresholdPercent = warningThresholdPercent;
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Enable(string? updatedBy = null)
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Disable(string? updatedBy = null)
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Checks if the current window has expired and resets if needed.
    /// </summary>
    public void CheckAndResetWindow()
    {
        if (TimeWindow == ThrottleTimeWindow.Concurrent)
            return; // Concurrent limits don't reset

        var windowDuration = GetWindowDuration();
        if (DateTime.UtcNow - WindowStartTime >= windowDuration)
        {
            CurrentUsage = 0;
            WindowStartTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Attempts to acquire a resource. Returns true if successful.
    /// </summary>
    public bool TryAcquire(int amount = 1)
    {
        if (!IsEnabled)
            return true;

        CheckAndResetWindow();

        if (CurrentUsage + amount <= MaxValue)
        {
            CurrentUsage += amount;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Releases a resource (for concurrent limits).
    /// </summary>
    public void Release(int amount = 1)
    {
        CurrentUsage = Math.Max(0, CurrentUsage - amount);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the current usage percentage.
    /// </summary>
    public double GetUsagePercent()
    {
        if (MaxValue == 0) return 0;
        return (double)CurrentUsage / MaxValue * 100;
    }

    /// <summary>
    /// Checks if usage is at or above the warning threshold.
    /// </summary>
    public bool IsAtWarningThreshold()
    {
        return GetUsagePercent() >= WarningThresholdPercent;
    }

    /// <summary>
    /// Checks if the throttle limit has been reached.
    /// </summary>
    public bool IsLimitReached()
    {
        return CurrentUsage >= MaxValue;
    }

    /// <summary>
    /// Gets the remaining capacity.
    /// </summary>
    public int GetRemainingCapacity()
    {
        return Math.Max(0, MaxValue - CurrentUsage);
    }

    /// <summary>
    /// Gets the time until the window resets.
    /// </summary>
    public TimeSpan GetTimeUntilReset()
    {
        if (TimeWindow == ThrottleTimeWindow.Concurrent)
            return TimeSpan.Zero;

        var windowDuration = GetWindowDuration();
        var elapsed = DateTime.UtcNow - WindowStartTime;
        var remaining = windowDuration - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private TimeSpan GetWindowDuration()
    {
        return TimeWindow switch
        {
            ThrottleTimeWindow.PerSecond => TimeSpan.FromSeconds(1),
            ThrottleTimeWindow.PerMinute => TimeSpan.FromMinutes(1),
            ThrottleTimeWindow.PerHour => TimeSpan.FromHours(1),
            ThrottleTimeWindow.PerDay => TimeSpan.FromDays(1),
            ThrottleTimeWindow.Concurrent => TimeSpan.Zero,
            _ => TimeSpan.FromMinutes(1)
        };
    }

    public void ResetUsage()
    {
        CurrentUsage = 0;
        WindowStartTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Defines the action to take when a throttle limit is reached.
/// </summary>
public enum ThrottleAction
{
    /// <summary>
    /// Reject the request immediately.
    /// </summary>
    Reject = 0,

    /// <summary>
    /// Queue the request for later processing.
    /// </summary>
    Queue = 1,

    /// <summary>
    /// Delay the request until capacity is available.
    /// </summary>
    Delay = 2,

    /// <summary>
    /// Log a warning but allow the request.
    /// </summary>
    LogOnly = 3
}
