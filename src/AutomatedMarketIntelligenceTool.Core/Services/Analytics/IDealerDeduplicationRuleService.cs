using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for managing dealer-specific deduplication rules.
/// </summary>
public interface IDealerDeduplicationRuleService
{
    /// <summary>
    /// Creates a new deduplication rule for a dealer.
    /// </summary>
    Task<DealerDeduplicationRule> CreateRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        string ruleName,
        string? description = null,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rules for a specific dealer.
    /// </summary>
    Task<List<DealerDeduplicationRule>> GetDealerRulesAsync(
        Guid tenantId,
        DealerId dealerId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    Task<DealerDeduplicationRule?> GetRuleByIdAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable rule for a listing from a dealer.
    /// Returns the highest priority active rule that matches the listing criteria.
    /// </summary>
    Task<DealerDeduplicationRule?> GetApplicableRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        decimal? price,
        int? year,
        string? make,
        string? model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a rule's thresholds.
    /// </summary>
    Task<DealerDeduplicationRule> UpdateRuleThresholdsAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        double? autoMatchThreshold,
        double? reviewThreshold,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a rule's field weights.
    /// </summary>
    Task<DealerDeduplicationRule> UpdateRuleWeightsAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        double? makeModelWeight = null,
        double? yearWeight = null,
        double? mileageWeight = null,
        double? priceWeight = null,
        double? locationWeight = null,
        double? imageWeight = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a rule's tolerance settings.
    /// </summary>
    Task<DealerDeduplicationRule> UpdateRuleTolerancesAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        int? mileageTolerance = null,
        decimal? priceTolerance = null,
        int? yearTolerance = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a rule's feature flags.
    /// </summary>
    Task<DealerDeduplicationRule> UpdateRuleFeaturesAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        bool? enableVinMatching = null,
        bool? enableFuzzyMatching = null,
        bool? enableImageMatching = null,
        bool? strictMode = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a rule.
    /// </summary>
    Task<DealerDeduplicationRule> ActivateRuleAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a rule.
    /// </summary>
    Task<DealerDeduplicationRule> DeactivateRuleAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a rule.
    /// </summary>
    Task DeleteRuleAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a strict VIN-only rule for a dealer.
    /// </summary>
    Task<DealerDeduplicationRule> CreateStrictVinOnlyRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a relaxed matching rule for trusted dealers.
    /// </summary>
    Task<DealerDeduplicationRule> CreateRelaxedRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a high-value vehicle rule for a dealer.
    /// </summary>
    Task<DealerDeduplicationRule> CreateHighValueRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        decimal minPrice,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a rule was applied.
    /// </summary>
    Task RecordRuleApplicationAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rule application statistics.
    /// </summary>
    Task<RuleApplicationStatistics> GetRuleStatisticsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rules across all dealers.
    /// </summary>
    Task<List<DealerDeduplicationRule>> GetAllRulesAsync(
        Guid tenantId,
        bool activeOnly = true,
        int? limit = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about rule applications.
/// </summary>
public class RuleApplicationStatistics
{
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int TotalApplications { get; set; }
    public Dictionary<string, int> ApplicationsByRuleName { get; set; } = new();
    public Dictionary<DealerId, int> ApplicationsByDealer { get; set; } = new();
    public List<DealerDeduplicationRule> MostUsedRules { get; set; } = new();
    public List<DealerDeduplicationRule> NeverUsedRules { get; set; } = new();
}
