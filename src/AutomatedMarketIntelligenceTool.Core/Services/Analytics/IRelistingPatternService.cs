using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for detecting and tracking relisting patterns.
/// </summary>
public interface IRelistingPatternService
{
    /// <summary>
    /// Detects if a listing is a relisting of a previous listing.
    /// </summary>
    Task<RelistingDetectionResult> DetectRelistingAsync(
        Guid tenantId,
        Listing currentListing,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting patterns for a specific dealer.
    /// </summary>
    Task<List<RelistingPattern>> GetDealerRelistingPatternsAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting patterns for a specific listing.
    /// </summary>
    Task<List<RelistingPattern>> GetListingRelistingHistoryAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all suspicious relisting patterns.
    /// </summary>
    Task<List<RelistingPattern>> GetSuspiciousPatternsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans all recent deactivated listings to detect relistings.
    /// </summary>
    Task<RelistingScanResult> ScanForRelistingsAsync(
        Guid tenantId,
        int lookbackDays = 30,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting statistics for a dealer.
    /// </summary>
    Task<DealerRelistingStatistics> GetDealerRelistingStatisticsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting statistics for the entire market.
    /// </summary>
    Task<MarketRelistingStatistics> GetMarketRelistingStatisticsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates dealers' frequent relister flags based on patterns.
    /// </summary>
    Task<int> UpdateFrequentRelisterFlagsAsync(
        Guid tenantId,
        double relistingRateThreshold = 0.20,
        int minRelistings = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a manually identified relisting pattern.
    /// </summary>
    Task<RelistingPattern> RecordManualRelistingAsync(
        Guid tenantId,
        ListingId currentListingId,
        ListingId previousListingId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of relisting detection.
/// </summary>
public class RelistingDetectionResult
{
    public bool IsRelisting { get; set; }
    public RelistingPattern? Pattern { get; set; }
    public ListingId? MatchedListingId { get; set; }
    public double MatchConfidence { get; set; }
    public string? MatchMethod { get; set; }
}

/// <summary>
/// Result of batch relisting scan.
/// </summary>
public class RelistingScanResult
{
    public int ListingsScanned { get; set; }
    public int RelistingsFound { get; set; }
    public int SuspiciousPatterns { get; set; }
    public long ProcessingTimeMs { get; set; }
    public List<RelistingPattern> Patterns { get; set; } = new();
}

/// <summary>
/// Relisting statistics for a specific dealer.
/// </summary>
public class DealerRelistingStatistics
{
    public DealerId DealerId { get; set; } = null!;
    public int TotalListings { get; set; }
    public int TotalRelistings { get; set; }
    public double RelistingRate { get; set; }
    public int SuspiciousRelistings { get; set; }
    public double AverageDaysBetweenRelistings { get; set; }
    public decimal AveragePriceChangeOnRelist { get; set; }
    public double AveragePriceChangePercentOnRelist { get; set; }
    public bool IsFrequentRelister { get; set; }
}

/// <summary>
/// Market-wide relisting statistics.
/// </summary>
public class MarketRelistingStatistics
{
    public int TotalListings { get; set; }
    public int TotalRelistings { get; set; }
    public double MarketRelistingRate { get; set; }
    public int SuspiciousRelistings { get; set; }
    public int FrequentRelisterCount { get; set; }
    public double AverageDaysBetweenRelistings { get; set; }
    public decimal AveragePriceChangeOnRelist { get; set; }
    public Dictionary<RelistingType, int> CountByType { get; set; } = new();
    public Dictionary<string, int> TopRelistingDealers { get; set; } = new();
}
