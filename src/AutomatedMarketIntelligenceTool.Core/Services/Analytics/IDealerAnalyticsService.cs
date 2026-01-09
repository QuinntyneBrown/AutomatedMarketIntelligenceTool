using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for analyzing dealer behavior and calculating reliability metrics.
/// </summary>
public interface IDealerAnalyticsService
{
    /// <summary>
    /// Calculates and updates analytics for a specific dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated dealer analytics.</returns>
    Task<DealerAnalytics> AnalyzeDealerAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes all dealers for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics for all dealers.</returns>
    Task<List<DealerAnalytics>> AnalyzeAllDealersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current analytics for a dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dealer analytics.</returns>
    Task<DealerAnalytics?> GetDealerAnalyticsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory history for a dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="fromDate">Start date for history.</param>
    /// <param name="toDate">End date for history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inventory history snapshots.</returns>
    Task<List<DealerInventorySnapshot>> GetInventoryHistoryAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dealers with low reliability scores.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="threshold">Reliability score threshold (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of dealers below threshold.</returns>
    Task<List<DealerAnalytics>> GetLowReliabilityDealersAsync(
        Guid tenantId,
        decimal threshold = 50m,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Analytics data for a dealer.
/// </summary>
public class DealerAnalytics
{
    public DealerId DealerId { get; init; } = null!;
    public string DealerName { get; init; } = string.Empty;
    public decimal ReliabilityScore { get; init; }
    public int AvgDaysOnMarket { get; init; }
    public int TotalListingsHistorical { get; init; }
    public int ActiveListings { get; init; }
    public int RelistedCount { get; init; }
    public bool FrequentRelisterFlag { get; init; }
    public DateTime? LastAnalyzedAt { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
}

/// <summary>
/// Snapshot of dealer inventory at a point in time.
/// </summary>
public class DealerInventorySnapshot
{
    public DateTime SnapshotDate { get; init; }
    public int TotalListings { get; init; }
    public int NewListings { get; init; }
    public int DeactivatedListings { get; init; }
    public decimal AveragePrice { get; init; }
    public int AverageMileage { get; init; }
    public Dictionary<string, int> MakeBreakdown { get; init; } = new();
}
