using Microsoft.AspNetCore.Mvc;
using Configuration.Api.DTOs;
using Configuration.Core.Entities;
using Configuration.Core.Interfaces;

namespace Configuration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfigurationService _configurationService;

    public ConfigController(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    // Service Configuration Endpoints

    [HttpGet("{service}")]
    public async Task<ActionResult<IEnumerable<ServiceConfigurationResponse>>> GetServiceConfig(string service, CancellationToken cancellationToken)
    {
        var configs = await _configurationService.GetServiceConfigAsync(service, cancellationToken);
        return Ok(configs.Select(MapToResponse));
    }

    [HttpGet("{service}/{key}")]
    public async Task<ActionResult<ServiceConfigurationResponse>> GetServiceConfigByKey(string service, string key, CancellationToken cancellationToken)
    {
        var config = await _configurationService.GetServiceConfigByKeyAsync(service, key, cancellationToken);
        if (config == null) return NotFound();
        return Ok(MapToResponse(config));
    }

    [HttpPut("{service}")]
    public async Task<ActionResult<ServiceConfigurationResponse>> SetServiceConfig(string service, [FromBody] SetServiceConfigRequest request, CancellationToken cancellationToken)
    {
        var config = await _configurationService.SetServiceConfigAsync(service, request.Key, request.Value, cancellationToken);
        return Ok(MapToResponse(config));
    }

    [HttpPut("{service}/bulk")]
    public async Task<ActionResult<IEnumerable<ServiceConfigurationResponse>>> BulkSetServiceConfig(string service, [FromBody] BulkSetServiceConfigRequest request, CancellationToken cancellationToken)
    {
        var results = new List<ServiceConfiguration>();
        foreach (var configRequest in request.Configs)
        {
            var config = await _configurationService.SetServiceConfigAsync(service, configRequest.Key, configRequest.Value, cancellationToken);
            results.Add(config);
        }
        return Ok(results.Select(MapToResponse));
    }

    [HttpDelete("{service}/{key}")]
    public async Task<IActionResult> DeleteServiceConfig(string service, string key, CancellationToken cancellationToken)
    {
        await _configurationService.DeleteServiceConfigAsync(service, key, cancellationToken);
        return NoContent();
    }

    // Feature Flag Endpoints

    [HttpGet("features")]
    public async Task<ActionResult<IEnumerable<FeatureFlagResponse>>> GetFeatureFlags(CancellationToken cancellationToken)
    {
        var flags = await _configurationService.GetFeatureFlagsAsync(cancellationToken);
        return Ok(flags.Select(MapToResponse));
    }

    [HttpGet("features/{flag}")]
    public async Task<ActionResult<FeatureFlagResponse>> GetFeatureFlag(string flag, CancellationToken cancellationToken)
    {
        var featureFlag = await _configurationService.GetFeatureFlagByNameAsync(flag, cancellationToken);
        if (featureFlag == null) return NotFound();
        return Ok(MapToResponse(featureFlag));
    }

    [HttpPost("features")]
    public async Task<ActionResult<FeatureFlagResponse>> CreateFeatureFlag([FromBody] CreateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var flag = await _configurationService.SetFeatureFlagAsync(request.Name, request.IsEnabled, request.Description, cancellationToken);
        return CreatedAtAction(nameof(GetFeatureFlag), new { flag = request.Name }, MapToResponse(flag));
    }

    [HttpPut("features/{flag}")]
    public async Task<ActionResult<FeatureFlagResponse>> SetFeatureFlag(string flag, [FromBody] SetFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var featureFlag = await _configurationService.SetFeatureFlagAsync(flag, request.IsEnabled, request.Description, cancellationToken);
        return Ok(MapToResponse(featureFlag));
    }

    [HttpPost("features/{flag}/toggle")]
    public async Task<ActionResult<FeatureFlagResponse>> ToggleFeatureFlag(string flag, CancellationToken cancellationToken)
    {
        try
        {
            var featureFlag = await _configurationService.ToggleFeatureFlagAsync(flag, cancellationToken);
            return Ok(MapToResponse(featureFlag));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("features/{flag}")]
    public async Task<IActionResult> DeleteFeatureFlag(string flag, CancellationToken cancellationToken)
    {
        await _configurationService.DeleteFeatureFlagAsync(flag, cancellationToken);
        return NoContent();
    }

    // Resource Throttle Endpoints

    [HttpGet("throttle")]
    public async Task<ActionResult<IEnumerable<ResourceThrottleResponse>>> GetThrottles(CancellationToken cancellationToken)
    {
        var throttles = await _configurationService.GetThrottlesAsync(cancellationToken);
        return Ok(throttles.Select(MapToResponse));
    }

    [HttpGet("throttle/{resource}")]
    public async Task<ActionResult<ResourceThrottleResponse>> GetThrottle(string resource, CancellationToken cancellationToken)
    {
        var throttle = await _configurationService.GetThrottleByNameAsync(resource, cancellationToken);
        if (throttle == null) return NotFound();
        return Ok(MapToResponse(throttle));
    }

    [HttpPost("throttle")]
    public async Task<ActionResult<ResourceThrottleResponse>> CreateThrottle([FromBody] CreateThrottleRequest request, CancellationToken cancellationToken)
    {
        var throttle = await _configurationService.CreateThrottleAsync(
            request.ResourceName,
            request.MaxConcurrent,
            request.RateLimitPerMinute,
            request.IsEnabled,
            cancellationToken);
        return CreatedAtAction(nameof(GetThrottle), new { resource = request.ResourceName }, MapToResponse(throttle));
    }

    [HttpPut("throttle/{resource}")]
    public async Task<ActionResult<ResourceThrottleResponse>> UpdateThrottle(string resource, [FromBody] UpdateThrottleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var throttle = await _configurationService.UpdateThrottleAsync(
                resource,
                request.MaxConcurrent,
                request.RateLimitPerMinute,
                request.IsEnabled,
                cancellationToken);
            return Ok(MapToResponse(throttle));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("throttle/{resource}")]
    public async Task<IActionResult> DeleteThrottle(string resource, CancellationToken cancellationToken)
    {
        await _configurationService.DeleteThrottleAsync(resource, cancellationToken);
        return NoContent();
    }

    // Mapping methods

    private static ServiceConfigurationResponse MapToResponse(ServiceConfiguration config)
    {
        return new ServiceConfigurationResponse(
            config.Id,
            config.ServiceName,
            config.Key,
            config.Value,
            config.Version,
            config.UpdatedAt);
    }

    private static FeatureFlagResponse MapToResponse(FeatureFlag flag)
    {
        return new FeatureFlagResponse(
            flag.Id,
            flag.Name,
            flag.IsEnabled,
            flag.Description,
            flag.UpdatedAt);
    }

    private static ResourceThrottleResponse MapToResponse(ResourceThrottle throttle)
    {
        return new ResourceThrottleResponse(
            throttle.Id,
            throttle.ResourceName,
            throttle.MaxConcurrent,
            throttle.RateLimitPerMinute,
            throttle.IsEnabled);
    }
}
