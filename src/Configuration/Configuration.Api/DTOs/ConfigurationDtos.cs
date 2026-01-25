namespace Configuration.Api.DTOs;

// Service Configuration DTOs
public sealed record ServiceConfigurationResponse(
    Guid Id,
    string ServiceName,
    string Key,
    string Value,
    int Version,
    DateTimeOffset UpdatedAt);

public sealed record SetServiceConfigRequest(
    string Key,
    string Value);

public sealed record BulkSetServiceConfigRequest(
    List<SetServiceConfigRequest> Configs);

// Feature Flag DTOs
public sealed record FeatureFlagResponse(
    Guid Id,
    string Name,
    bool IsEnabled,
    string? Description,
    DateTimeOffset UpdatedAt);

public sealed record SetFeatureFlagRequest(
    bool IsEnabled,
    string? Description = null);

public sealed record CreateFeatureFlagRequest(
    string Name,
    bool IsEnabled = false,
    string? Description = null);

// Resource Throttle DTOs
public sealed record ResourceThrottleResponse(
    Guid Id,
    string ResourceName,
    int MaxConcurrent,
    int RateLimitPerMinute,
    bool IsEnabled);

public sealed record UpdateThrottleRequest(
    int? MaxConcurrent = null,
    int? RateLimitPerMinute = null,
    bool? IsEnabled = null);

public sealed record CreateThrottleRequest(
    string ResourceName,
    int MaxConcurrent,
    int RateLimitPerMinute,
    bool IsEnabled = true);
