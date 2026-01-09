using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for managing deduplication configuration including dealer-specific rules.
/// </summary>
public interface IDeduplicationConfigService
{
    /// <summary>
    /// Gets the deduplication threshold for a specific dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The threshold value (0-100) or null if using default.</returns>
    Task<decimal?> GetDealerThresholdAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a custom deduplication threshold for a dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="threshold">The threshold value (0-100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetDealerThresholdAsync(
        Guid tenantId,
        DealerId dealerId,
        decimal threshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes custom threshold for a dealer (reverts to default).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDealerThresholdAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all dealers with custom thresholds.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of dealer-specific configurations.</returns>
    Task<List<DealerDeduplicationConfig>> GetAllDealerConfigsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables strict mode (VIN-only) for a dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="enabled">Whether strict mode is enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetDealerStrictModeAsync(
        Guid tenantId,
        DealerId dealerId,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether strict mode is enabled for a dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strict mode is enabled, false otherwise.</returns>
    Task<bool> GetDealerStrictModeAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Deduplication configuration for a dealer.
/// </summary>
public class DealerDeduplicationConfig
{
    public DealerId DealerId { get; init; } = null!;
    public string DealerName { get; init; } = string.Empty;
    public decimal? CustomThreshold { get; init; }
    public bool StrictMode { get; init; }
    public DateTime ConfiguredAt { get; init; }
    public string? ConfiguredBy { get; init; }
}
