using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for analyzing dealer performance and calculating reliability metrics.
/// </summary>
public interface IDealerAnalyticsService
{
    /// <summary>
    /// Gets or creates dealer metrics for a specific dealer.
    /// </summary>
    Task<DealerMetrics> GetOrCreateDealerMetricsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dealer metrics by dealer ID.
    /// </summary>
    Task<DealerMetrics?> GetDealerMetricsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a dealer and updates their metrics.
    /// </summary>
    Task<DealerMetrics> AnalyzeDealerAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes all dealers that are due for analysis.
    /// </summary>
    Task<DealerAnalysisBatchResult> AnalyzeAllDealersAsync(
        Guid tenantId,
        bool forceReanalysis = false,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dealers by reliability score range.
    /// </summary>
    Task<List<DealerWithMetrics>> GetDealersByReliabilityScoreAsync(
        Guid tenantId,
        decimal minScore,
        decimal maxScore,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dealers flagged as frequent relisters.
    /// </summary>
    Task<List<DealerWithMetrics>> GetFrequentRelistersAsync(
        Guid tenantId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top dealers by various criteria.
    /// </summary>
    Task<List<DealerWithMetrics>> GetTopDealersAsync(
        Guid tenantId,
        DealerRankingCriteria criteria,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dealers that need re-analysis based on activity.
    /// </summary>
    Task<List<DealerId>> GetDealersNeedingAnalysisAsync(
        Guid tenantId,
        TimeSpan analysisInterval,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates aggregate market statistics for dealer metrics.
    /// </summary>
    Task<DealerMarketStatistics> GetDealerMarketStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Criteria for ranking dealers.
/// </summary>
public enum DealerRankingCriteria
{
    ReliabilityScore,
    LowestDaysOnMarket,
    HighestVolume,
    BestDataQuality,
    LowestRelistingRate
}

/// <summary>
/// Result of batch dealer analysis.
/// </summary>
public class DealerAnalysisBatchResult
{
    public int TotalDealersAnalyzed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long ProcessingTimeMs { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Dealer with associated metrics.
/// </summary>
public class DealerWithMetrics
{
    public required Dealer Dealer { get; init; }
    public DealerMetrics? Metrics { get; init; }
}

/// <summary>
/// Aggregate statistics about dealers in the market.
/// </summary>
public class DealerMarketStatistics
{
    public int TotalDealers { get; set; }
    public int ActiveDealers { get; set; }
    public decimal AverageReliabilityScore { get; set; }
    public decimal MedianReliabilityScore { get; set; }
    public int FrequentRelisterCount { get; set; }
    public double AverageDaysOnMarket { get; set; }
    public int TotalActiveListings { get; set; }
    public Dictionary<string, int> DealersByState { get; set; } = new();
    public Dictionary<string, int> DealersByCity { get; set; } = new();
}

/// <summary>
/// Progress information for batch operations.
/// </summary>
public class BatchProgress
{
    public int Current { get; set; }
    public int Total { get; set; }
    public string? Message { get; set; }
    public double PercentComplete => Total > 0 ? (double)Current / Total * 100 : 0;

    public BatchProgress() { }

    public BatchProgress(int current, int total, string? message = null)
    {
        Current = current;
        Total = total;
        Message = message;
    }
}
