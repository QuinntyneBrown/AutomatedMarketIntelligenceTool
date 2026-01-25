namespace Dashboard.Core.Models;

/// <summary>
/// Represents market trend analytics.
/// </summary>
public sealed class MarketTrends
{
    /// <summary>
    /// Average price across all active listings.
    /// </summary>
    public decimal AveragePrice { get; init; }

    /// <summary>
    /// Median price across all active listings.
    /// </summary>
    public decimal MedianPrice { get; init; }

    /// <summary>
    /// Price change percentage compared to previous period.
    /// </summary>
    public decimal PriceChangePercent { get; init; }

    /// <summary>
    /// Number of listings by source.
    /// </summary>
    public IReadOnlyDictionary<string, int> ListingsBySource { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Number of listings by make.
    /// </summary>
    public IReadOnlyDictionary<string, int> ListingsByMake { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Number of listings by province.
    /// </summary>
    public IReadOnlyDictionary<string, int> ListingsByProvince { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Average price by make.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> AveragePriceByMake { get; init; } = new Dictionary<string, decimal>();

    /// <summary>
    /// Average mileage across all active listings.
    /// </summary>
    public int? AverageMileage { get; init; }

    /// <summary>
    /// Most common year in listings.
    /// </summary>
    public int? MostCommonYear { get; init; }

    /// <summary>
    /// Timestamp when these trends were calculated.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;
}
