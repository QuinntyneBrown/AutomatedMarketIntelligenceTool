using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;

/// <summary>
/// Represents custom deduplication rules for a specific dealer.
/// Allows fine-tuning deduplication thresholds and behaviors
/// on a per-dealer basis.
/// </summary>
public class DealerDeduplicationRule
{
    public DealerDeduplicationRuleId DealerDeduplicationRuleId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public DealerId DealerId { get; private set; } = null!;

    // Rule identification
    public string RuleName { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }

    // Matching thresholds (override global settings)
    public double? AutoMatchThreshold { get; private set; }
    public double? ReviewThreshold { get; private set; }

    // Field weights (override global settings)
    public double? MakeModelWeight { get; private set; }
    public double? YearWeight { get; private set; }
    public double? MileageWeight { get; private set; }
    public double? PriceWeight { get; private set; }
    public double? LocationWeight { get; private set; }
    public double? ImageWeight { get; private set; }

    // Tolerance settings
    public int? MileageTolerance { get; private set; }
    public decimal? PriceTolerance { get; private set; }
    public int? YearTolerance { get; private set; }

    // Feature flags
    public bool? EnableVinMatching { get; private set; }
    public bool? EnableFuzzyMatching { get; private set; }
    public bool? EnableImageMatching { get; private set; }
    public bool? StrictModeEnabled { get; private set; }

    // Conditions for rule application
    public RuleCondition Condition { get; private set; }
    public decimal? MinPrice { get; private set; }
    public decimal? MaxPrice { get; private set; }
    public int? MinYear { get; private set; }
    public int? MaxYear { get; private set; }
    public string? MakeFilter { get; private set; }
    public string? ModelFilter { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }
    public int TimesApplied { get; private set; }
    public DateTime? LastAppliedAt { get; private set; }

    private DealerDeduplicationRule() { }

    public static DealerDeduplicationRule Create(
        Guid tenantId,
        DealerId dealerId,
        string ruleName,
        string? description = null,
        string? createdBy = null)
    {
        return new DealerDeduplicationRule
        {
            DealerDeduplicationRuleId = DealerDeduplicationRuleId.CreateNew(),
            TenantId = tenantId,
            DealerId = dealerId,
            RuleName = ruleName,
            Description = description,
            IsActive = true,
            Priority = 0,
            Condition = RuleCondition.Always,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            TimesApplied = 0
        };
    }

    /// <summary>
    /// Creates a strict VIN-only rule for a dealer.
    /// Useful for dealers with known data quality issues.
    /// </summary>
    public static DealerDeduplicationRule CreateStrictVinOnlyRule(
        Guid tenantId,
        DealerId dealerId,
        string? createdBy = null)
    {
        var rule = Create(tenantId, dealerId, "Strict VIN-Only Matching",
            "Only matches listings by exact VIN match", createdBy);

        rule.StrictModeEnabled = true;
        rule.EnableVinMatching = true;
        rule.EnableFuzzyMatching = false;
        rule.EnableImageMatching = false;
        rule.Priority = 100; // High priority

        return rule;
    }

    /// <summary>
    /// Creates a relaxed matching rule for trusted dealers.
    /// </summary>
    public static DealerDeduplicationRule CreateRelaxedRule(
        Guid tenantId,
        DealerId dealerId,
        string? createdBy = null)
    {
        var rule = Create(tenantId, dealerId, "Relaxed Matching",
            "Lower thresholds for trusted dealers with good data quality", createdBy);

        rule.AutoMatchThreshold = 75.0;
        rule.ReviewThreshold = 50.0;
        rule.MileageTolerance = 1000;
        rule.PriceTolerance = 1000m;
        rule.Priority = 50;

        return rule;
    }

    /// <summary>
    /// Creates a high-value vehicle rule with stricter matching.
    /// </summary>
    public static DealerDeduplicationRule CreateHighValueRule(
        Guid tenantId,
        DealerId dealerId,
        decimal minPrice,
        string? createdBy = null)
    {
        var rule = Create(tenantId, dealerId, "High-Value Vehicle Matching",
            $"Stricter matching for vehicles priced above ${minPrice:N0}", createdBy);

        rule.Condition = RuleCondition.PriceRange;
        rule.MinPrice = minPrice;
        rule.AutoMatchThreshold = 95.0;
        rule.ReviewThreshold = 80.0;
        rule.MileageTolerance = 100;
        rule.PriceTolerance = 100m;
        rule.EnableImageMatching = true;
        rule.ImageWeight = 0.30;
        rule.Priority = 75;

        return rule;
    }

    public void SetThresholds(double? autoMatch, double? review)
    {
        AutoMatchThreshold = autoMatch;
        ReviewThreshold = review;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFieldWeights(
        double? makeModel = null,
        double? year = null,
        double? mileage = null,
        double? price = null,
        double? location = null,
        double? image = null)
    {
        MakeModelWeight = makeModel;
        YearWeight = year;
        MileageWeight = mileage;
        PriceWeight = price;
        LocationWeight = location;
        ImageWeight = image;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTolerances(int? mileage = null, decimal? price = null, int? year = null)
    {
        MileageTolerance = mileage;
        PriceTolerance = price;
        YearTolerance = year;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFeatureFlags(bool? vinMatching = null, bool? fuzzyMatching = null,
        bool? imageMatching = null, bool? strictMode = null)
    {
        EnableVinMatching = vinMatching;
        EnableFuzzyMatching = fuzzyMatching;
        EnableImageMatching = imageMatching;
        StrictModeEnabled = strictMode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCondition(RuleCondition condition, decimal? minPrice = null,
        decimal? maxPrice = null, int? minYear = null, int? maxYear = null,
        string? makeFilter = null, string? modelFilter = null)
    {
        Condition = condition;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        MinYear = minYear;
        MaxYear = maxYear;
        MakeFilter = makeFilter;
        ModelFilter = modelFilter;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordApplication()
    {
        TimesApplied++;
        LastAppliedAt = DateTime.UtcNow;
    }

    public void SetUpdatedBy(string? updatedBy)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this rule applies to the given listing criteria.
    /// </summary>
    public bool AppliesToListing(decimal? price, int? year, string? make, string? model)
    {
        if (!IsActive) return false;

        return Condition switch
        {
            RuleCondition.Always => true,
            RuleCondition.PriceRange => CheckPriceRange(price),
            RuleCondition.YearRange => CheckYearRange(year),
            RuleCondition.MakeModel => CheckMakeModel(make, model),
            RuleCondition.Combined => CheckPriceRange(price) && CheckYearRange(year) && CheckMakeModel(make, model),
            _ => true
        };
    }

    private bool CheckPriceRange(decimal? price)
    {
        if (!price.HasValue) return true;
        if (MinPrice.HasValue && price.Value < MinPrice.Value) return false;
        if (MaxPrice.HasValue && price.Value > MaxPrice.Value) return false;
        return true;
    }

    private bool CheckYearRange(int? year)
    {
        if (!year.HasValue) return true;
        if (MinYear.HasValue && year.Value < MinYear.Value) return false;
        if (MaxYear.HasValue && year.Value > MaxYear.Value) return false;
        return true;
    }

    private bool CheckMakeModel(string? make, string? model)
    {
        if (!string.IsNullOrEmpty(MakeFilter) &&
            !string.Equals(make, MakeFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(ModelFilter) &&
            !string.Equals(model, ModelFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}

/// <summary>
/// Conditions under which a dealer deduplication rule applies.
/// </summary>
public enum RuleCondition
{
    /// <summary>Rule always applies to this dealer</summary>
    Always,

    /// <summary>Rule applies based on price range</summary>
    PriceRange,

    /// <summary>Rule applies based on year range</summary>
    YearRange,

    /// <summary>Rule applies based on make/model</summary>
    MakeModel,

    /// <summary>Rule applies when all conditions are met</summary>
    Combined
}
