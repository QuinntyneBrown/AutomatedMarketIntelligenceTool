using Microsoft.EntityFrameworkCore;
using Configuration.Core.Entities;
using Configuration.Core.Interfaces;
using Configuration.Infrastructure.Data;

namespace Configuration.Infrastructure.Services;

public sealed class ConfigurationService : IConfigurationService
{
    private readonly ConfigurationDbContext _context;

    public ConfigurationService(ConfigurationDbContext context)
    {
        _context = context;
    }

    // Service Configuration
    public async Task<IReadOnlyList<ServiceConfiguration>> GetServiceConfigAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceConfigurations
            .Where(c => c.ServiceName == serviceName)
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceConfiguration?> GetServiceConfigByKeyAsync(string serviceName, string key, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceConfigurations
            .FirstOrDefaultAsync(c => c.ServiceName == serviceName && c.Key == key, cancellationToken);
    }

    public async Task<ServiceConfiguration> SetServiceConfigAsync(string serviceName, string key, string value, CancellationToken cancellationToken = default)
    {
        var config = await GetServiceConfigByKeyAsync(serviceName, key, cancellationToken);

        if (config == null)
        {
            config = ServiceConfiguration.Create(serviceName, key, value);
            _context.ServiceConfigurations.Add(config);
        }
        else
        {
            config.UpdateValue(value);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task DeleteServiceConfigAsync(string serviceName, string key, CancellationToken cancellationToken = default)
    {
        var config = await GetServiceConfigByKeyAsync(serviceName, key, cancellationToken);
        if (config != null)
        {
            _context.ServiceConfigurations.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Feature Flags
    public async Task<IReadOnlyList<FeatureFlag>> GetFeatureFlagsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeatureFlag?> GetFeatureFlagByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);
    }

    public async Task<FeatureFlag> ToggleFeatureFlagAsync(string name, CancellationToken cancellationToken = default)
    {
        var flag = await GetFeatureFlagByNameAsync(name, cancellationToken)
            ?? throw new InvalidOperationException($"Feature flag '{name}' not found");

        flag.Toggle();
        await _context.SaveChangesAsync(cancellationToken);
        return flag;
    }

    public async Task<FeatureFlag> SetFeatureFlagAsync(string name, bool isEnabled, string? description = null, CancellationToken cancellationToken = default)
    {
        var flag = await GetFeatureFlagByNameAsync(name, cancellationToken);

        if (flag == null)
        {
            flag = FeatureFlag.Create(name, isEnabled, description);
            _context.FeatureFlags.Add(flag);
        }
        else
        {
            flag.SetEnabled(isEnabled);
            if (description != null)
            {
                flag.UpdateDescription(description);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return flag;
    }

    public async Task DeleteFeatureFlagAsync(string name, CancellationToken cancellationToken = default)
    {
        var flag = await GetFeatureFlagByNameAsync(name, cancellationToken);
        if (flag != null)
        {
            _context.FeatureFlags.Remove(flag);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Resource Throttles
    public async Task<IReadOnlyList<ResourceThrottle>> GetThrottlesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ResourceThrottles
            .OrderBy(t => t.ResourceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResourceThrottle?> GetThrottleByNameAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceThrottles
            .FirstOrDefaultAsync(t => t.ResourceName == resourceName, cancellationToken);
    }

    public async Task<ResourceThrottle> UpdateThrottleAsync(string resourceName, int? maxConcurrent = null, int? rateLimitPerMinute = null, bool? isEnabled = null, CancellationToken cancellationToken = default)
    {
        var throttle = await GetThrottleByNameAsync(resourceName, cancellationToken)
            ?? throw new InvalidOperationException($"Resource throttle '{resourceName}' not found");

        throttle.Update(maxConcurrent, rateLimitPerMinute, isEnabled);
        await _context.SaveChangesAsync(cancellationToken);
        return throttle;
    }

    public async Task<ResourceThrottle> CreateThrottleAsync(string resourceName, int maxConcurrent, int rateLimitPerMinute, bool isEnabled = true, CancellationToken cancellationToken = default)
    {
        var existing = await GetThrottleByNameAsync(resourceName, cancellationToken);
        if (existing != null)
        {
            existing.Update(maxConcurrent, rateLimitPerMinute, isEnabled);
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var throttle = ResourceThrottle.Create(resourceName, maxConcurrent, rateLimitPerMinute, isEnabled);
        _context.ResourceThrottles.Add(throttle);
        await _context.SaveChangesAsync(cancellationToken);
        return throttle;
    }

    public async Task DeleteThrottleAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        var throttle = await GetThrottleByNameAsync(resourceName, cancellationToken);
        if (throttle != null)
        {
            _context.ResourceThrottles.Remove(throttle);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
