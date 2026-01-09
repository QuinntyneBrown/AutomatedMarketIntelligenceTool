using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Implementation of deduplication configuration service.
/// Note: This is a simplified in-memory implementation for Phase 5.
/// A full implementation would use database persistence.
/// </summary>
public class DeduplicationConfigService : IDeduplicationConfigService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DeduplicationConfigService> _logger;

    // In-memory storage for dealer-specific configs
    // Key: "tenantId:dealerId"
    private static readonly ConcurrentDictionary<string, DealerConfigData> _configs = new();

    public DeduplicationConfigService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DeduplicationConfigService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<decimal?> GetDealerThresholdAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(tenantId, dealerId);
        if (_configs.TryGetValue(key, out var config))
        {
            return Task.FromResult(config.CustomThreshold);
        }
        return Task.FromResult<decimal?>(null);
    }

    public Task SetDealerThresholdAsync(
        Guid tenantId,
        DealerId dealerId,
        decimal threshold,
        CancellationToken cancellationToken = default)
    {
        if (threshold < 0 || threshold > 100)
        {
            throw new ArgumentException("Threshold must be between 0 and 100.", nameof(threshold));
        }

        var key = GetKey(tenantId, dealerId);
        _configs.AddOrUpdate(key,
            _ => new DealerConfigData
            {
                TenantId = tenantId,
                DealerId = dealerId,
                CustomThreshold = threshold,
                ConfiguredAt = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.CustomThreshold = threshold;
                existing.ConfiguredAt = DateTime.UtcNow;
                return existing;
            });

        _logger.LogInformation(
            "Set custom threshold {Threshold} for dealer {DealerId} in tenant {TenantId}",
            threshold, dealerId.Value, tenantId);

        return Task.CompletedTask;
    }

    public Task RemoveDealerThresholdAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(tenantId, dealerId);
        if (_configs.TryGetValue(key, out var config))
        {
            config.CustomThreshold = null;
            config.ConfiguredAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Removed custom threshold for dealer {DealerId} in tenant {TenantId}",
                dealerId.Value, tenantId);
        }

        return Task.CompletedTask;
    }

    public async Task<List<DealerDeduplicationConfig>> GetAllDealerConfigsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenantConfigs = _configs
            .Where(kvp => kvp.Value.TenantId == tenantId)
            .ToList();

        var results = new List<DealerDeduplicationConfig>();

        foreach (var config in tenantConfigs)
        {
            var dealer = await _context.Dealers
                .FirstOrDefaultAsync(d => d.DealerId == config.Value.DealerId && d.TenantId == tenantId, cancellationToken);

            if (dealer != null)
            {
                results.Add(new DealerDeduplicationConfig
                {
                    DealerId = config.Value.DealerId,
                    DealerName = dealer.Name,
                    CustomThreshold = config.Value.CustomThreshold,
                    StrictMode = config.Value.StrictMode,
                    ConfiguredAt = config.Value.ConfiguredAt,
                    ConfiguredBy = config.Value.ConfiguredBy
                });
            }
        }

        return results;
    }

    public Task SetDealerStrictModeAsync(
        Guid tenantId,
        DealerId dealerId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(tenantId, dealerId);
        _configs.AddOrUpdate(key,
            _ => new DealerConfigData
            {
                TenantId = tenantId,
                DealerId = dealerId,
                StrictMode = enabled,
                ConfiguredAt = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.StrictMode = enabled;
                existing.ConfiguredAt = DateTime.UtcNow;
                return existing;
            });

        _logger.LogInformation(
            "Set strict mode to {Enabled} for dealer {DealerId} in tenant {TenantId}",
            enabled, dealerId.Value, tenantId);

        return Task.CompletedTask;
    }

    public Task<bool> GetDealerStrictModeAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(tenantId, dealerId);
        if (_configs.TryGetValue(key, out var config))
        {
            return Task.FromResult(config.StrictMode);
        }
        return Task.FromResult(false);
    }

    private static string GetKey(Guid tenantId, DealerId dealerId)
    {
        return $"{tenantId}:{dealerId.Value}";
    }

    private class DealerConfigData
    {
        public Guid TenantId { get; set; }
        public DealerId DealerId { get; set; } = null!;
        public decimal? CustomThreshold { get; set; }
        public bool StrictMode { get; set; }
        public DateTime ConfiguredAt { get; set; }
        public string? ConfiguredBy { get; set; }
    }
}
