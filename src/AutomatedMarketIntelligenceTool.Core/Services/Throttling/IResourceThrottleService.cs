using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Throttling;

/// <summary>
/// Service for managing resource throttling configurations.
/// </summary>
public interface IResourceThrottleService
{
    /// <summary>
    /// Creates a new resource throttle configuration.
    /// </summary>
    Task<ResourceThrottle> CreateThrottleAsync(
        Guid tenantId,
        ResourceType resourceType,
        int maxValue,
        ThrottleTimeWindow timeWindow,
        ThrottleAction action = ThrottleAction.Reject,
        int warningThresholdPercent = 80,
        string? description = null,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a throttle configuration by ID.
    /// </summary>
    Task<ResourceThrottle?> GetThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a throttle configuration by resource type.
    /// </summary>
    Task<ResourceThrottle?> GetThrottleByTypeAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all throttle configurations for a tenant.
    /// </summary>
    Task<IReadOnlyList<ResourceThrottle>> GetAllThrottlesAsync(
        Guid tenantId,
        bool enabledOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a throttle configuration.
    /// </summary>
    Task<ResourceThrottle> UpdateThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        int maxValue,
        ThrottleTimeWindow timeWindow,
        ThrottleAction action,
        int warningThresholdPercent,
        string? description = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a throttle.
    /// </summary>
    Task<bool> EnableThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a throttle.
    /// </summary>
    Task<bool> DisableThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a throttle configuration.
    /// </summary>
    Task<bool> DeleteThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a resource. Returns result indicating success/failure.
    /// </summary>
    Task<ThrottleResult> TryAcquireAsync(
        Guid tenantId,
        ResourceType resourceType,
        int amount = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a resource (for concurrent limits).
    /// </summary>
    Task ReleaseAsync(
        Guid tenantId,
        ResourceType resourceType,
        int amount = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current usage status for all throttles.
    /// </summary>
    Task<IReadOnlyList<ThrottleStatus>> GetStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the usage for a specific throttle.
    /// </summary>
    Task<bool> ResetUsageAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes default throttles for a tenant.
    /// </summary>
    Task InitializeDefaultThrottlesAsync(
        Guid tenantId,
        string? createdBy = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a throttle acquisition attempt.
/// </summary>
public class ThrottleResult
{
    public bool IsAllowed { get; init; }
    public ResourceType ResourceType { get; init; }
    public ThrottleAction Action { get; init; }
    public int CurrentUsage { get; init; }
    public int MaxValue { get; init; }
    public TimeSpan? TimeUntilReset { get; init; }
    public string? Message { get; init; }

    public static ThrottleResult Allowed(ResourceType resourceType, int currentUsage, int maxValue) => new()
    {
        IsAllowed = true,
        ResourceType = resourceType,
        Action = ThrottleAction.LogOnly,
        CurrentUsage = currentUsage,
        MaxValue = maxValue
    };

    public static ThrottleResult NoThrottle(ResourceType resourceType) => new()
    {
        IsAllowed = true,
        ResourceType = resourceType,
        Action = ThrottleAction.LogOnly,
        Message = "No throttle configured for this resource type"
    };

    public static ThrottleResult Rejected(
        ResourceType resourceType,
        ThrottleAction action,
        int currentUsage,
        int maxValue,
        TimeSpan? timeUntilReset) => new()
    {
        IsAllowed = false,
        ResourceType = resourceType,
        Action = action,
        CurrentUsage = currentUsage,
        MaxValue = maxValue,
        TimeUntilReset = timeUntilReset,
        Message = $"Rate limit exceeded for {resourceType}. Current: {currentUsage}, Max: {maxValue}"
    };
}

/// <summary>
/// Current status of a throttle.
/// </summary>
public class ThrottleStatus
{
    public ResourceThrottleId ThrottleId { get; init; } = null!;
    public ResourceType ResourceType { get; init; }
    public int CurrentUsage { get; init; }
    public int MaxValue { get; init; }
    public double UsagePercent { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsAtWarningThreshold { get; init; }
    public bool IsLimitReached { get; init; }
    public int RemainingCapacity { get; init; }
    public TimeSpan TimeUntilReset { get; init; }
    public ThrottleTimeWindow TimeWindow { get; init; }
    public ThrottleAction Action { get; init; }
}
