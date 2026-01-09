using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

/// <summary>
/// Service for managing deduplication configuration settings.
/// </summary>
public class DeduplicationConfigService : IDeduplicationConfigService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DeduplicationConfigService> _logger;

    public DeduplicationConfigService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DeduplicationConfigService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeduplicationConfig?> GetConfigAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be empty", nameof(key));

        return await _context.DeduplicationConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.ConfigKey == key,
                cancellationToken);
    }

    public async Task<IReadOnlyList<DeduplicationConfig>> GetAllConfigsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DeduplicationConfigs
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeduplicationConfig>> GetConfigsByPatternAsync(
        Guid tenantId,
        string keyPattern,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyPattern))
            throw new ArgumentException("Key pattern cannot be empty", nameof(keyPattern));

        // Convert wildcard pattern to SQL LIKE pattern
        var likePattern = keyPattern.Replace("*", "%");

        return await _context.DeduplicationConfigs
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && EF.Functions.Like(c.ConfigKey, likePattern))
            .OrderBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeduplicationConfig> SetConfigAsync(
        Guid tenantId,
        string key,
        string value,
        string? description = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be empty", nameof(key));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var existing = await _context.DeduplicationConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.ConfigKey == key,
                cancellationToken);

        if (existing != null)
        {
            existing.UpdateValue(value, updatedBy);
            if (description != null)
                existing.UpdateDescription(description);

            _logger.LogInformation(
                "Updated deduplication config {Key} for tenant {TenantId}",
                key, tenantId);
        }
        else
        {
            existing = DeduplicationConfig.Create(tenantId, key, value, description, updatedBy);
            _context.DeduplicationConfigs.Add(existing);

            _logger.LogInformation(
                "Created deduplication config {Key} for tenant {TenantId}",
                key, tenantId);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteConfigAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be empty", nameof(key));

        var config = await _context.DeduplicationConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.ConfigKey == key,
                cancellationToken);

        if (config == null)
            return false;

        _context.DeduplicationConfigs.Remove(config);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted deduplication config {Key} for tenant {TenantId}",
            key, tenantId);

        return true;
    }

    public async Task<DeduplicationOptions> GetOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var configs = await GetAllConfigsAsync(tenantId, cancellationToken);
        var configDict = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);

        return new DeduplicationOptions
        {
            Enabled = GetBoolValue(configDict, DeduplicationConfig.Keys.Enabled, DeduplicationConfig.Defaults.Enabled),
            AutoThreshold = GetDoubleValue(configDict, DeduplicationConfig.Keys.AutoThreshold, DeduplicationConfig.Defaults.AutoThreshold),
            ReviewThreshold = GetDoubleValue(configDict, DeduplicationConfig.Keys.ReviewThreshold, DeduplicationConfig.Defaults.ReviewThreshold),
            StrictMode = GetBoolValue(configDict, DeduplicationConfig.Keys.StrictMode, DeduplicationConfig.Defaults.StrictMode),
            EnableFuzzyMatching = GetBoolValue(configDict, DeduplicationConfig.Keys.EnableFuzzyMatching, DeduplicationConfig.Defaults.EnableFuzzyMatching),
            EnableImageMatching = GetBoolValue(configDict, DeduplicationConfig.Keys.EnableImageMatching, DeduplicationConfig.Defaults.EnableImageMatching),
            MileageTolerance = GetIntValue(configDict, DeduplicationConfig.Keys.MileageTolerance, DeduplicationConfig.Defaults.MileageTolerance),
            PriceTolerance = GetDecimalValue(configDict, DeduplicationConfig.Keys.PriceTolerance, DeduplicationConfig.Defaults.PriceTolerance),
            Weights = new FieldWeights
            {
                MakeModel = GetDoubleValue(configDict, DeduplicationConfig.Keys.WeightMakeModel, DeduplicationConfig.Defaults.WeightMakeModel),
                Year = GetDoubleValue(configDict, DeduplicationConfig.Keys.WeightYear, DeduplicationConfig.Defaults.WeightYear),
                Mileage = GetDoubleValue(configDict, DeduplicationConfig.Keys.WeightMileage, DeduplicationConfig.Defaults.WeightMileage),
                Price = GetDoubleValue(configDict, DeduplicationConfig.Keys.WeightPrice, DeduplicationConfig.Defaults.WeightPrice),
                Location = GetDoubleValue(configDict, DeduplicationConfig.Keys.WeightLocation, DeduplicationConfig.Defaults.WeightLocation),
                Image = GetDoubleValue(configDict, DeduplicationConfig.Keys.WeightImage, DeduplicationConfig.Defaults.WeightImage)
            }
        };
    }

    public async Task ResetToDefaultsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var configs = await _context.DeduplicationConfigs
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        _context.DeduplicationConfigs.RemoveRange(configs);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reset all deduplication configs to defaults for tenant {TenantId}",
            tenantId);
    }

    private static bool GetBoolValue(Dictionary<string, string> configs, string key, bool defaultValue)
    {
        if (configs.TryGetValue(key, out var value) && bool.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    private static double GetDoubleValue(Dictionary<string, string> configs, string key, double defaultValue)
    {
        if (configs.TryGetValue(key, out var value) && double.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    private static int GetIntValue(Dictionary<string, string> configs, string key, int defaultValue)
    {
        if (configs.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    private static decimal GetDecimalValue(Dictionary<string, string> configs, string key, decimal defaultValue)
    {
        if (configs.TryGetValue(key, out var value) && decimal.TryParse(value, out var result))
            return result;
        return defaultValue;
    }
}
