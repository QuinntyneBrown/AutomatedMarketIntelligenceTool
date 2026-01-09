using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Service for detecting and analyzing relisting patterns.
/// </summary>
public interface IRelistingPatternService
{
    /// <summary>
    /// Analyzes relisting patterns for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Relisting pattern analysis.</returns>
    Task<RelistingPatternAnalysis> AnalyzePatternsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting patterns for a specific dealer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="dealerId">The dealer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dealer relisting patterns.</returns>
    Task<DealerRelistingPattern> GetDealerPatternsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies frequent relisters across all dealers.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="threshold">Minimum relist count to be considered frequent (default 3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of frequent relister dealers.</returns>
    Task<List<FrequentRelister>> GetFrequentRelistersAsync(
        Guid tenantId,
        int threshold = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting trends over time.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Time-series relisting data.</returns>
    Task<List<RelistingTrend>> GetRelistingTrendsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Overall relisting pattern analysis for a tenant.
/// </summary>
public class RelistingPatternAnalysis
{
    public int TotalRelistedVehicles { get; init; }
    public int FrequentRelistersCount { get; init; }
    public double AverageRelistCount { get; init; }
    public double MedianTimeOffMarket { get; init; }
    public decimal MedianPriceChange { get; init; }
    public List<string> TopRelistingMakes { get; init; } = new();
    public List<FrequentRelister> TopFrequentRelisters { get; init; } = new();
}

/// <summary>
/// Relisting pattern for a specific dealer.
/// </summary>
public class DealerRelistingPattern
{
    public DealerId DealerId { get; init; } = null!;
    public string DealerName { get; init; } = string.Empty;
    public int TotalRelistedListings { get; init; }
    public double AverageRelistsPerVehicle { get; init; }
    public double AverageTimeOffMarketDays { get; init; }
    public decimal AveragePriceChangePercent { get; init; }
    public bool IsFrequentRelister { get; init; }
    public List<RelistingExample> RecentExamples { get; init; } = new();
}

/// <summary>
/// Information about a frequent relister.
/// </summary>
public class FrequentRelister
{
    public DealerId DealerId { get; init; } = null!;
    public string DealerName { get; init; } = string.Empty;
    public int TotalRelistedCount { get; init; }
    public int VehiclesRelistedMultipleTimes { get; init; }
    public double AverageRelistCount { get; init; }
    public decimal AveragePriceReduction { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
}

/// <summary>
/// Example of a relisted vehicle.
/// </summary>
public class RelistingExample
{
    public ListingId ListingId { get; init; } = null!;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public int RelistCount { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal PriceChange { get; init; }
    public DateTime FirstSeenAt { get; init; }
    public DateTime? LastRelistedAt { get; init; }
}

/// <summary>
/// Relisting trend data point.
/// </summary>
public class RelistingTrend
{
    public DateTime Date { get; init; }
    public int RelistCount { get; init; }
    public int UniqueVehicles { get; init; }
    public decimal AveragePriceChange { get; init; }
    public double AverageDaysOffMarket { get; init; }
}
