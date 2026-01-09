using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

/// <summary>
/// Service for managing deduplication configuration settings.
/// </summary>
public interface IDeduplicationConfigService
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    Task<DeduplicationConfig?> GetConfigAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values for a tenant.
    /// </summary>
    Task<IReadOnlyList<DeduplicationConfig>> GetAllConfigsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration values matching a key pattern (e.g., "dedup.weight.*").
    /// </summary>
    Task<IReadOnlyList<DeduplicationConfig>> GetConfigsByPatternAsync(
        Guid tenantId,
        string keyPattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    Task<DeduplicationConfig> SetConfigAsync(
        Guid tenantId,
        string key,
        string value,
        string? description = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration value.
    /// </summary>
    Task<bool> DeleteConfigAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the resolved deduplication options for a tenant.
    /// Combines stored config with defaults.
    /// </summary>
    Task<DeduplicationOptions> GetOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all configuration to defaults for a tenant.
    /// </summary>
    Task ResetToDefaultsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolved deduplication options combining stored config with defaults.
/// </summary>
public class DeduplicationOptions
{
    public bool Enabled { get; init; } = DeduplicationConfig.Defaults.Enabled;
    public double AutoThreshold { get; init; } = DeduplicationConfig.Defaults.AutoThreshold;
    public double ReviewThreshold { get; init; } = DeduplicationConfig.Defaults.ReviewThreshold;
    public bool StrictMode { get; init; } = DeduplicationConfig.Defaults.StrictMode;
    public bool EnableFuzzyMatching { get; init; } = DeduplicationConfig.Defaults.EnableFuzzyMatching;
    public bool EnableImageMatching { get; init; } = DeduplicationConfig.Defaults.EnableImageMatching;
    public int MileageTolerance { get; init; } = DeduplicationConfig.Defaults.MileageTolerance;
    public decimal PriceTolerance { get; init; } = DeduplicationConfig.Defaults.PriceTolerance;
    public FieldWeights Weights { get; init; } = new();

    public static DeduplicationOptions Default => new();
}

/// <summary>
/// Field weights for fuzzy matching.
/// </summary>
public class FieldWeights
{
    public double MakeModel { get; init; } = DeduplicationConfig.Defaults.WeightMakeModel;
    public double Year { get; init; } = DeduplicationConfig.Defaults.WeightYear;
    public double Mileage { get; init; } = DeduplicationConfig.Defaults.WeightMileage;
    public double Price { get; init; } = DeduplicationConfig.Defaults.WeightPrice;
    public double Location { get; init; } = DeduplicationConfig.Defaults.WeightLocation;
    public double Image { get; init; } = DeduplicationConfig.Defaults.WeightImage;

    /// <summary>
    /// Validates that weights sum to approximately 1.0.
    /// </summary>
    public bool IsValid()
    {
        var sum = MakeModel + Year + Mileage + Price + Location + Image;
        return Math.Abs(sum - 1.0) < 0.01;
    }
}
