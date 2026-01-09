namespace AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;

/// <summary>
/// Represents a deduplication configuration setting.
/// </summary>
public class DeduplicationConfig
{
    public DeduplicationConfigId ConfigId { get; private set; } = null!;
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The configuration key (e.g., "dedup.threshold", "dedup.strict-mode").
    /// </summary>
    public string ConfigKey { get; private set; } = null!;

    /// <summary>
    /// The configuration value (stored as JSON for complex values).
    /// </summary>
    public string ConfigValue { get; private set; } = null!;

    /// <summary>
    /// Optional description of the configuration.
    /// </summary>
    public string? Description { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    private DeduplicationConfig() { }

    public static DeduplicationConfig Create(
        Guid tenantId,
        string configKey,
        string configValue,
        string? description = null,
        string? updatedBy = null)
    {
        return new DeduplicationConfig
        {
            ConfigId = DeduplicationConfigId.CreateNew(),
            TenantId = tenantId,
            ConfigKey = configKey,
            ConfigValue = configValue,
            Description = description,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };
    }

    public void UpdateValue(string newValue, string? updatedBy = null)
    {
        ConfigValue = newValue;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }

    // Well-known configuration keys
    public static class Keys
    {
        public const string Enabled = "dedup.enabled";
        public const string AutoThreshold = "dedup.auto-threshold";
        public const string ReviewThreshold = "dedup.review-threshold";
        public const string StrictMode = "dedup.strict-mode";
        public const string EnableFuzzyMatching = "dedup.fuzzy-enabled";
        public const string EnableImageMatching = "dedup.image-enabled";
        public const string MileageTolerance = "dedup.mileage-tolerance";
        public const string PriceTolerance = "dedup.price-tolerance";

        // Field weights
        public const string WeightMakeModel = "dedup.weight.make-model";
        public const string WeightYear = "dedup.weight.year";
        public const string WeightMileage = "dedup.weight.mileage";
        public const string WeightPrice = "dedup.weight.price";
        public const string WeightLocation = "dedup.weight.location";
        public const string WeightImage = "dedup.weight.image";
    }

    // Default values
    public static class Defaults
    {
        public const bool Enabled = true;
        public const double AutoThreshold = 85.0;
        public const double ReviewThreshold = 60.0;
        public const bool StrictMode = false;
        public const bool EnableFuzzyMatching = true;
        public const bool EnableImageMatching = false;
        public const int MileageTolerance = 500;
        public const decimal PriceTolerance = 500m;

        // Field weights (should sum to 1.0)
        public const double WeightMakeModel = 0.30;
        public const double WeightYear = 0.20;
        public const double WeightMileage = 0.15;
        public const double WeightPrice = 0.15;
        public const double WeightLocation = 0.10;
        public const double WeightImage = 0.10;
    }
}
