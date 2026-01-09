using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;

/// <summary>
/// Service for managing dealer-specific deduplication rules.
/// </summary>
public class DealerDeduplicationRuleService : IDealerDeduplicationRuleService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DealerDeduplicationRuleService> _logger;

    public DealerDeduplicationRuleService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DealerDeduplicationRuleService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DealerDeduplicationRule> CreateRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        string ruleName,
        string? description = null,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = DealerDeduplicationRule.Create(tenantId, dealerId, ruleName, description, createdBy);

        _context.DealerDeduplicationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created deduplication rule '{RuleName}' for dealer {DealerId}",
            ruleName, dealerId.Value);

        return rule;
    }

    public async Task<List<DealerDeduplicationRule>> GetDealerRulesAsync(
        Guid tenantId,
        DealerId dealerId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DealerDeduplicationRules
            .Where(r => r.TenantId == tenantId && r.DealerId == dealerId);

        if (activeOnly)
            query = query.Where(r => r.IsActive);

        return await query
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<DealerDeduplicationRule?> GetRuleByIdAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DealerDeduplicationRules
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.DealerDeduplicationRuleId == ruleId,
                cancellationToken);
    }

    public async Task<DealerDeduplicationRule?> GetApplicableRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        decimal? price,
        int? year,
        string? make,
        string? model,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetDealerRulesAsync(tenantId, dealerId, activeOnly: true, cancellationToken);

        // Return the highest priority rule that applies
        return rules.FirstOrDefault(r => r.AppliesToListing(price, year, make, model));
    }

    public async Task<DealerDeduplicationRule> UpdateRuleThresholdsAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        double? autoMatchThreshold,
        double? reviewThreshold,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        rule.SetThresholds(autoMatchThreshold, reviewThreshold);
        rule.SetUpdatedBy(updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated thresholds for rule {RuleId}: Auto={Auto}, Review={Review}",
            ruleId.Value, autoMatchThreshold, reviewThreshold);

        return rule;
    }

    public async Task<DealerDeduplicationRule> UpdateRuleWeightsAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        double? makeModelWeight = null,
        double? yearWeight = null,
        double? mileageWeight = null,
        double? priceWeight = null,
        double? locationWeight = null,
        double? imageWeight = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        rule.SetFieldWeights(makeModelWeight, yearWeight, mileageWeight, priceWeight, locationWeight, imageWeight);
        rule.SetUpdatedBy(updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated field weights for rule {RuleId}",
            ruleId.Value);

        return rule;
    }

    public async Task<DealerDeduplicationRule> UpdateRuleTolerancesAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        int? mileageTolerance = null,
        decimal? priceTolerance = null,
        int? yearTolerance = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        rule.SetTolerances(mileageTolerance, priceTolerance, yearTolerance);
        rule.SetUpdatedBy(updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated tolerances for rule {RuleId}: Mileage={Mileage}, Price={Price}, Year={Year}",
            ruleId.Value, mileageTolerance, priceTolerance, yearTolerance);

        return rule;
    }

    public async Task<DealerDeduplicationRule> UpdateRuleFeaturesAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        bool? enableVinMatching = null,
        bool? enableFuzzyMatching = null,
        bool? enableImageMatching = null,
        bool? strictMode = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        rule.SetFeatureFlags(enableVinMatching, enableFuzzyMatching, enableImageMatching, strictMode);
        rule.SetUpdatedBy(updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated feature flags for rule {RuleId}",
            ruleId.Value);

        return rule;
    }

    public async Task<DealerDeduplicationRule> ActivateRuleAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        rule.Activate();
        rule.SetUpdatedBy(updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated rule {RuleId}", ruleId.Value);

        return rule;
    }

    public async Task<DealerDeduplicationRule> DeactivateRuleAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        rule.Deactivate();
        rule.SetUpdatedBy(updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated rule {RuleId}", ruleId.Value);

        return rule;
    }

    public async Task DeleteRuleAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken)
            ?? throw new ArgumentException($"Rule {ruleId.Value} not found");

        _context.DealerDeduplicationRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted rule {RuleId}", ruleId.Value);
    }

    public async Task<DealerDeduplicationRule> CreateStrictVinOnlyRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = DealerDeduplicationRule.CreateStrictVinOnlyRule(tenantId, dealerId, createdBy);

        _context.DealerDeduplicationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created strict VIN-only rule for dealer {DealerId}",
            dealerId.Value);

        return rule;
    }

    public async Task<DealerDeduplicationRule> CreateRelaxedRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = DealerDeduplicationRule.CreateRelaxedRule(tenantId, dealerId, createdBy);

        _context.DealerDeduplicationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created relaxed matching rule for dealer {DealerId}",
            dealerId.Value);

        return rule;
    }

    public async Task<DealerDeduplicationRule> CreateHighValueRuleAsync(
        Guid tenantId,
        DealerId dealerId,
        decimal minPrice,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var rule = DealerDeduplicationRule.CreateHighValueRule(tenantId, dealerId, minPrice, createdBy);

        _context.DealerDeduplicationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created high-value vehicle rule for dealer {DealerId} (min price: ${MinPrice})",
            dealerId.Value, minPrice);

        return rule;
    }

    public async Task RecordRuleApplicationAsync(
        Guid tenantId,
        DealerDeduplicationRuleId ruleId,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(tenantId, ruleId, cancellationToken);
        if (rule != null)
        {
            rule.RecordApplication();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<RuleApplicationStatistics> GetRuleStatisticsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await _context.DealerDeduplicationRules
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // Filter by date if needed
        if (fromDate.HasValue || toDate.HasValue)
        {
            rules = rules.Where(r =>
            {
                if (r.LastAppliedAt == null) return false;
                if (fromDate.HasValue && r.LastAppliedAt < fromDate.Value) return false;
                if (toDate.HasValue && r.LastAppliedAt > toDate.Value) return false;
                return true;
            }).ToList();
        }

        var stats = new RuleApplicationStatistics
        {
            TotalRules = rules.Count,
            ActiveRules = rules.Count(r => r.IsActive),
            TotalApplications = rules.Sum(r => r.TimesApplied),
            ApplicationsByRuleName = rules
                .GroupBy(r => r.RuleName)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TimesApplied)),
            ApplicationsByDealer = rules
                .GroupBy(r => r.DealerId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TimesApplied)),
            MostUsedRules = rules
                .Where(r => r.TimesApplied > 0)
                .OrderByDescending(r => r.TimesApplied)
                .Take(10)
                .ToList(),
            NeverUsedRules = rules
                .Where(r => r.TimesApplied == 0)
                .OrderBy(r => r.CreatedAt)
                .ToList()
        };

        return stats;
    }

    public async Task<List<DealerDeduplicationRule>> GetAllRulesAsync(
        Guid tenantId,
        bool activeOnly = true,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DealerDeduplicationRules
            .Where(r => r.TenantId == tenantId);

        if (activeOnly)
            query = query.Where(r => r.IsActive);

        query = query.OrderByDescending(r => r.Priority).ThenBy(r => r.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }
}
