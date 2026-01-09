using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Throttling;

/// <summary>
/// Service for managing resource throttling configurations.
/// </summary>
public class ResourceThrottleService : IResourceThrottleService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<ResourceThrottleService> _logger;

    public ResourceThrottleService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<ResourceThrottleService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResourceThrottle> CreateThrottleAsync(
        Guid tenantId,
        ResourceType resourceType,
        int maxValue,
        ThrottleTimeWindow timeWindow,
        ThrottleAction action = ThrottleAction.Reject,
        int warningThresholdPercent = 80,
        string? description = null,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        // Check for existing throttle of same type
        var existing = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceType == resourceType,
                cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"A throttle for resource type '{resourceType}' already exists for this tenant");
        }

        var throttle = ResourceThrottle.Create(
            tenantId,
            resourceType,
            maxValue,
            timeWindow,
            action,
            warningThresholdPercent,
            description,
            createdBy);

        _context.ResourceThrottles.Add(throttle);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created resource throttle {ThrottleId} for {ResourceType} with max {MaxValue} {TimeWindow} for tenant {TenantId}",
            throttle.ResourceThrottleId.Value, resourceType, maxValue, timeWindow, tenantId);

        return throttle;
    }

    public async Task<ResourceThrottle?> GetThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceThrottleId == throttleId,
                cancellationToken);
    }

    public async Task<ResourceThrottle?> GetThrottleByTypeAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default)
    {
        return await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceType == resourceType,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceThrottle>> GetAllThrottlesAsync(
        Guid tenantId,
        bool enabledOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceThrottles
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId);

        if (enabledOnly)
        {
            query = query.Where(t => t.IsEnabled);
        }

        return await query
            .OrderBy(t => t.ResourceType)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResourceThrottle> UpdateThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        int maxValue,
        ThrottleTimeWindow timeWindow,
        ThrottleAction action,
        int warningThresholdPercent,
        string? description = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceThrottleId == throttleId,
                cancellationToken);

        if (throttle == null)
        {
            throw new InvalidOperationException($"Resource throttle {throttleId} not found");
        }

        throttle.Update(maxValue, timeWindow, action, warningThresholdPercent, description, updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated resource throttle {ThrottleId} for {ResourceType} with max {MaxValue} {TimeWindow}",
            throttle.ResourceThrottleId.Value, throttle.ResourceType, maxValue, timeWindow);

        return throttle;
    }

    public async Task<bool> EnableThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceThrottleId == throttleId,
                cancellationToken);

        if (throttle == null)
            return false;

        throttle.Enable(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Enabled resource throttle {ThrottleId} for {ResourceType}",
            throttle.ResourceThrottleId.Value, throttle.ResourceType);

        return true;
    }

    public async Task<bool> DisableThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceThrottleId == throttleId,
                cancellationToken);

        if (throttle == null)
            return false;

        throttle.Disable(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Disabled resource throttle {ThrottleId} for {ResourceType}",
            throttle.ResourceThrottleId.Value, throttle.ResourceType);

        return true;
    }

    public async Task<bool> DeleteThrottleAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceThrottleId == throttleId,
                cancellationToken);

        if (throttle == null)
            return false;

        _context.ResourceThrottles.Remove(throttle);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted resource throttle {ThrottleId} for {ResourceType}",
            throttle.ResourceThrottleId.Value, throttle.ResourceType);

        return true;
    }

    public async Task<ThrottleResult> TryAcquireAsync(
        Guid tenantId,
        ResourceType resourceType,
        int amount = 1,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceType == resourceType && t.IsEnabled,
                cancellationToken);

        if (throttle == null)
        {
            return ThrottleResult.NoThrottle(resourceType);
        }

        throttle.CheckAndResetWindow();

        if (throttle.TryAcquire(amount))
        {
            await _context.SaveChangesAsync(cancellationToken);

            if (throttle.IsAtWarningThreshold())
            {
                _logger.LogWarning(
                    "Resource {ResourceType} is at warning threshold: {UsagePercent:F1}% ({Current}/{Max})",
                    resourceType, throttle.GetUsagePercent(), throttle.CurrentUsage, throttle.MaxValue);
            }

            return ThrottleResult.Allowed(resourceType, throttle.CurrentUsage, throttle.MaxValue);
        }

        _logger.LogWarning(
            "Resource throttle limit reached for {ResourceType}: {Current}/{Max}",
            resourceType, throttle.CurrentUsage, throttle.MaxValue);

        return ThrottleResult.Rejected(
            resourceType,
            throttle.Action,
            throttle.CurrentUsage,
            throttle.MaxValue,
            throttle.GetTimeUntilReset());
    }

    public async Task ReleaseAsync(
        Guid tenantId,
        ResourceType resourceType,
        int amount = 1,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceType == resourceType,
                cancellationToken);

        if (throttle != null)
        {
            throttle.Release(amount);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ThrottleStatus>> GetStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var throttles = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return throttles.Select(t =>
        {
            t.CheckAndResetWindow();
            return new ThrottleStatus
            {
                ThrottleId = t.ResourceThrottleId,
                ResourceType = t.ResourceType,
                CurrentUsage = t.CurrentUsage,
                MaxValue = t.MaxValue,
                UsagePercent = t.GetUsagePercent(),
                IsEnabled = t.IsEnabled,
                IsAtWarningThreshold = t.IsAtWarningThreshold(),
                IsLimitReached = t.IsLimitReached(),
                RemainingCapacity = t.GetRemainingCapacity(),
                TimeUntilReset = t.GetTimeUntilReset(),
                TimeWindow = t.TimeWindow,
                Action = t.Action
            };
        }).ToList();
    }

    public async Task<bool> ResetUsageAsync(
        Guid tenantId,
        ResourceThrottleId throttleId,
        CancellationToken cancellationToken = default)
    {
        var throttle = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.ResourceThrottleId == throttleId,
                cancellationToken);

        if (throttle == null)
            return false;

        throttle.ResetUsage();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reset usage for resource throttle {ThrottleId} for {ResourceType}",
            throttle.ResourceThrottleId.Value, throttle.ResourceType);

        return true;
    }

    public async Task InitializeDefaultThrottlesAsync(
        Guid tenantId,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var existingTypes = await _context.ResourceThrottles
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId)
            .Select(t => t.ResourceType)
            .ToListAsync(cancellationToken);

        var defaults = GetDefaultThrottles();

        foreach (var (resourceType, maxValue, timeWindow, description) in defaults)
        {
            if (existingTypes.Contains(resourceType))
                continue;

            var throttle = ResourceThrottle.Create(
                tenantId,
                resourceType,
                maxValue,
                timeWindow,
                ThrottleAction.Reject,
                80,
                description,
                createdBy);

            _context.ResourceThrottles.Add(throttle);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Initialized default throttles for tenant {TenantId}",
            tenantId);
    }

    private static IEnumerable<(ResourceType, int, ThrottleTimeWindow, string)> GetDefaultThrottles()
    {
        yield return (ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute, "API request rate limit");
        yield return (ResourceType.ConcurrentScrapers, 5, ThrottleTimeWindow.Concurrent, "Concurrent scraping operations");
        yield return (ResourceType.ReportGenerations, 100, ThrottleTimeWindow.PerDay, "Daily report generation limit");
        yield return (ResourceType.DatabaseQueries, 500, ThrottleTimeWindow.PerMinute, "Database query rate limit");
        yield return (ResourceType.ExportOperations, 50, ThrottleTimeWindow.PerDay, "Daily export operations limit");
        yield return (ResourceType.EmailNotifications, 100, ThrottleTimeWindow.PerDay, "Daily email notification limit");
    }
}
