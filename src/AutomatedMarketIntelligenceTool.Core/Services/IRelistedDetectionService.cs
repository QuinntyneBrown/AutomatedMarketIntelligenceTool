using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for detecting when previously deactivated listings reappear.
/// </summary>
public interface IRelistedDetectionService
{
    /// <summary>
    /// Checks if a listing appears to be a relisted version of a previously seen listing.
    /// </summary>
    /// <param name="scrapedListing">The newly scraped listing info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Relisting detection result.</returns>
    Task<RelistingCheckResult> CheckForRelistingAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relisting statistics for a tenant.
    /// </summary>
    Task<RelistingStats> GetStatsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all relisted listings for a tenant.
    /// </summary>
    Task<IReadOnlyList<RelistedListingInfo>> GetRelistedListingsAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a relisting check.
/// </summary>
public class RelistingCheckResult
{
    /// <summary>
    /// Whether this listing was previously seen and deactivated.
    /// </summary>
    public bool IsRelisted { get; init; }

    /// <summary>
    /// The previous listing if this is a relisting.
    /// </summary>
    public Listing? PreviousListing { get; init; }

    /// <summary>
    /// The previous listing ID.
    /// </summary>
    public Guid? PreviousListingId { get; init; }

    /// <summary>
    /// How long the listing was off market.
    /// </summary>
    public TimeSpan? TimeOffMarket { get; init; }

    /// <summary>
    /// Price change from previous listing.
    /// </summary>
    public decimal? PriceDelta { get; init; }

    /// <summary>
    /// Price change percentage from previous listing.
    /// </summary>
    public double? PriceChangePercentage { get; init; }

    /// <summary>
    /// How many times this vehicle has been relisted.
    /// </summary>
    public int TotalRelistCount { get; init; }

    /// <summary>
    /// Match confidence (how sure we are this is the same vehicle).
    /// </summary>
    public double MatchConfidence { get; init; }

    /// <summary>
    /// Creates a result indicating this is not a relisting.
    /// </summary>
    public static RelistingCheckResult NotRelisted() => new()
    {
        IsRelisted = false
    };

    /// <summary>
    /// Creates a result indicating this is a relisting.
    /// </summary>
    public static RelistingCheckResult Relisted(
        Listing previousListing,
        TimeSpan timeOffMarket,
        decimal priceDelta,
        double matchConfidence)
    {
        var priceChangePercentage = previousListing.Price != 0
            ? (double)(priceDelta / previousListing.Price) * 100
            : 0;

        return new RelistingCheckResult
        {
            IsRelisted = true,
            PreviousListing = previousListing,
            PreviousListingId = previousListing.ListingId.Value,
            TimeOffMarket = timeOffMarket,
            PriceDelta = priceDelta,
            PriceChangePercentage = priceChangePercentage,
            TotalRelistCount = previousListing.RelistedCount + 1,
            MatchConfidence = matchConfidence
        };
    }
}

/// <summary>
/// Statistics about relisted vehicles.
/// </summary>
public class RelistingStats
{
    public int TotalRelistedCount { get; init; }
    public int ActiveRelistedCount { get; init; }
    public double AverageTimeOffMarketDays { get; init; }
    public decimal AveragePriceDelta { get; init; }
    public double AveragePriceChangePercentage { get; init; }
    public int FrequentRelisterCount { get; init; }  // Listings relisted 3+ times
}

/// <summary>
/// Information about a relisted listing.
/// </summary>
public class RelistedListingInfo
{
    public ListingId ListingId { get; init; } = null!;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal? PreviousPrice { get; init; }
    public decimal? PriceDelta { get; init; }
    public int RelistedCount { get; init; }
    public DateTime? DeactivatedAt { get; init; }
    public DateTime ReactivatedAt { get; init; }
    public TimeSpan? TimeOffMarket { get; init; }
    public string SourceSite { get; init; } = string.Empty;
}
