using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for tracking and analyzing dealer inventory history.
/// </summary>
public interface IInventoryHistoryService
{
    /// <summary>
    /// Creates a snapshot of a dealer's current inventory.
    /// </summary>
    Task<InventorySnapshot> CreateSnapshotAsync(
        Guid tenantId,
        DealerId dealerId,
        SnapshotPeriodType periodType = SnapshotPeriodType.Daily,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates snapshots for all dealers.
    /// </summary>
    Task<SnapshotBatchResult> CreateSnapshotsForAllDealersAsync(
        Guid tenantId,
        SnapshotPeriodType periodType = SnapshotPeriodType.Daily,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent snapshot for a dealer.
    /// </summary>
    Task<InventorySnapshot?> GetLatestSnapshotAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets snapshots for a dealer within a date range.
    /// </summary>
    Task<List<InventorySnapshot>> GetDealerSnapshotsAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        SnapshotPeriodType? periodType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets snapshot summaries for a dealer (lightweight).
    /// </summary>
    Task<List<InventorySnapshotSummary>> GetDealerSnapshotSummariesAsync(
        Guid tenantId,
        DealerId dealerId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory trends for a dealer.
    /// </summary>
    Task<InventoryTrendData> GetInventoryTrendsAsync(
        Guid tenantId,
        DealerId dealerId,
        int periodDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory comparison between two dates.
    /// </summary>
    Task<InventoryComparison> CompareInventoryAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime date1,
        DateTime date2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old snapshots based on retention policy.
    /// </summary>
    Task<int> CleanupOldSnapshotsAsync(
        Guid tenantId,
        int retentionDays = 365,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregate inventory statistics across all dealers.
    /// </summary>
    Task<MarketInventoryStatistics> GetMarketInventoryStatisticsAsync(
        Guid tenantId,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dealers with significant inventory changes.
    /// </summary>
    Task<List<DealerInventoryChange>> GetDealersWithSignificantChangesAsync(
        Guid tenantId,
        double changeThresholdPercent = 20.0,
        int periodDays = 7,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes aggregated weekly/monthly snapshots from daily data.
    /// </summary>
    Task<int> AggregateSnapshotsAsync(
        Guid tenantId,
        SnapshotPeriodType targetPeriod,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of batch snapshot creation.
/// </summary>
public class SnapshotBatchResult
{
    public int TotalDealers { get; set; }
    public int SnapshotsCreated { get; set; }
    public int Failures { get; set; }
    public long ProcessingTimeMs { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Trend data for inventory analysis.
/// </summary>
public class InventoryTrendData
{
    public DealerId DealerId { get; set; } = null!;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<InventoryDataPoint> DataPoints { get; set; } = new();
    public TrendDirection InventoryTrend { get; set; }
    public TrendDirection ValueTrend { get; set; }
    public double InventoryChangePercent { get; set; }
    public double ValueChangePercent { get; set; }
    public double AverageTurnoverRate { get; set; }
}

/// <summary>
/// Single data point in trend analysis.
/// </summary>
public class InventoryDataPoint
{
    public DateTime Date { get; set; }
    public int TotalListings { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AveragePrice { get; set; }
    public int NewListings { get; set; }
    public int RemovedListings { get; set; }
}

/// <summary>
/// Direction of a trend.
/// </summary>
public enum TrendDirection
{
    Increasing,
    Stable,
    Decreasing
}

/// <summary>
/// Comparison between two inventory snapshots.
/// </summary>
public class InventoryComparison
{
    public DealerId DealerId { get; set; } = null!;
    public DateTime Date1 { get; set; }
    public DateTime Date2 { get; set; }
    public int ListingsDate1 { get; set; }
    public int ListingsDate2 { get; set; }
    public int ListingChange { get; set; }
    public double ListingChangePercent { get; set; }
    public decimal ValueDate1 { get; set; }
    public decimal ValueDate2 { get; set; }
    public decimal ValueChange { get; set; }
    public double ValueChangePercent { get; set; }
    public int ListingsAdded { get; set; }
    public int ListingsRemoved { get; set; }
}

/// <summary>
/// Market-wide inventory statistics.
/// </summary>
public class MarketInventoryStatistics
{
    public DateTime AsOfDate { get; set; }
    public int TotalDealers { get; set; }
    public int TotalListings { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal AverageListingPrice { get; set; }
    public double AverageDaysOnMarket { get; set; }
    public int NewListingsLast7Days { get; set; }
    public int RemovedListingsLast7Days { get; set; }
    public Dictionary<string, int> ListingsByMake { get; set; } = new();
    public Dictionary<int, int> ListingsByYear { get; set; } = new();
}

/// <summary>
/// Represents a significant inventory change for a dealer.
/// </summary>
public class DealerInventoryChange
{
    public DealerId DealerId { get; set; } = null!;
    public string DealerName { get; set; } = null!;
    public int PreviousListingCount { get; set; }
    public int CurrentListingCount { get; set; }
    public int ChangeAmount { get; set; }
    public double ChangePercent { get; set; }
    public ChangeType Type { get; set; }
}

/// <summary>
/// Type of inventory change.
/// </summary>
public enum ChangeType
{
    SignificantIncrease,
    SignificantDecrease,
    MassAddition,
    MassRemoval
}
