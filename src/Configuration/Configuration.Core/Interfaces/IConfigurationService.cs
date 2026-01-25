using Configuration.Core.Entities;

namespace Configuration.Core.Interfaces;

public interface IConfigurationService
{
    // Service Configuration
    Task<IReadOnlyList<ServiceConfiguration>> GetServiceConfigAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<ServiceConfiguration?> GetServiceConfigByKeyAsync(string serviceName, string key, CancellationToken cancellationToken = default);
    Task<ServiceConfiguration> SetServiceConfigAsync(string serviceName, string key, string value, CancellationToken cancellationToken = default);
    Task DeleteServiceConfigAsync(string serviceName, string key, CancellationToken cancellationToken = default);

    // Feature Flags
    Task<IReadOnlyList<FeatureFlag>> GetFeatureFlagsAsync(CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetFeatureFlagByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<FeatureFlag> ToggleFeatureFlagAsync(string name, CancellationToken cancellationToken = default);
    Task<FeatureFlag> SetFeatureFlagAsync(string name, bool isEnabled, string? description = null, CancellationToken cancellationToken = default);
    Task DeleteFeatureFlagAsync(string name, CancellationToken cancellationToken = default);

    // Resource Throttles
    Task<IReadOnlyList<ResourceThrottle>> GetThrottlesAsync(CancellationToken cancellationToken = default);
    Task<ResourceThrottle?> GetThrottleByNameAsync(string resourceName, CancellationToken cancellationToken = default);
    Task<ResourceThrottle> UpdateThrottleAsync(string resourceName, int? maxConcurrent = null, int? rateLimitPerMinute = null, bool? isEnabled = null, CancellationToken cancellationToken = default);
    Task<ResourceThrottle> CreateThrottleAsync(string resourceName, int maxConcurrent, int rateLimitPerMinute, bool isEnabled = true, CancellationToken cancellationToken = default);
    Task DeleteThrottleAsync(string resourceName, CancellationToken cancellationToken = default);
}
