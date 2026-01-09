using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;

/// <summary>
/// Represents a detected relisting pattern linking a current listing
/// to its previous incarnation. Used for tracking dealer behavior
/// and market manipulation detection.
/// </summary>
public class RelistingPattern
{
    public RelistingPatternId RelistingPatternId { get; private set; } = null!;
    public Guid TenantId { get; private set; }

    // Linked listings
    public ListingId CurrentListingId { get; private set; } = null!;
    public ListingId PreviousListingId { get; private set; } = null!;

    // Dealer information
    public DealerId? DealerId { get; private set; }

    // Pattern detection details
    public RelistingType Type { get; private set; }
    public double MatchConfidence { get; private set; }
    public string MatchMethod { get; private set; } = null!;

    // Price changes
    public decimal? PreviousPrice { get; private set; }
    public decimal? CurrentPrice { get; private set; }
    public decimal? PriceChange { get; private set; }
    public double? PriceChangePercent { get; private set; }

    // Timing information
    public DateTime PreviousDeactivatedAt { get; private set; }
    public DateTime CurrentListedAt { get; private set; }
    public int DaysBetweenListings { get; private set; }
    public int PreviousDaysOnMarket { get; private set; }

    // Vehicle information (for quick reference without joins)
    public string? Vin { get; private set; }
    public string? Make { get; private set; }
    public string? Model { get; private set; }
    public int? Year { get; private set; }

    // Flags
    public bool IsSuspiciousPattern { get; private set; }
    public string? SuspiciousReason { get; private set; }

    // Timestamps
    public DateTime DetectedAt { get; private set; }

    private RelistingPattern() { }

    public static RelistingPattern Create(
        Guid tenantId,
        ListingId currentListingId,
        ListingId previousListingId,
        DealerId? dealerId,
        RelistingType type,
        double matchConfidence,
        string matchMethod,
        decimal? previousPrice,
        decimal? currentPrice,
        DateTime previousDeactivatedAt,
        DateTime currentListedAt,
        int previousDaysOnMarket,
        string? vin = null,
        string? make = null,
        string? model = null,
        int? year = null)
    {
        var pattern = new RelistingPattern
        {
            RelistingPatternId = RelistingPatternId.CreateNew(),
            TenantId = tenantId,
            CurrentListingId = currentListingId,
            PreviousListingId = previousListingId,
            DealerId = dealerId,
            Type = type,
            MatchConfidence = matchConfidence,
            MatchMethod = matchMethod,
            PreviousPrice = previousPrice,
            CurrentPrice = currentPrice,
            PreviousDeactivatedAt = previousDeactivatedAt,
            CurrentListedAt = currentListedAt,
            PreviousDaysOnMarket = previousDaysOnMarket,
            Vin = vin,
            Make = make,
            Model = model,
            Year = year,
            DetectedAt = DateTime.UtcNow
        };

        // Calculate price change
        if (previousPrice.HasValue && currentPrice.HasValue)
        {
            pattern.PriceChange = currentPrice.Value - previousPrice.Value;
            if (previousPrice.Value != 0)
            {
                pattern.PriceChangePercent = (double)(pattern.PriceChange.Value / previousPrice.Value) * 100;
            }
        }

        // Calculate days between listings
        pattern.DaysBetweenListings = (int)(currentListedAt - previousDeactivatedAt).TotalDays;

        // Analyze for suspicious patterns
        pattern.AnalyzeForSuspiciousPatterns();

        return pattern;
    }

    /// <summary>
    /// Analyzes the pattern to determine if it's suspicious.
    /// </summary>
    private void AnalyzeForSuspiciousPatterns()
    {
        var reasons = new List<string>();

        // 1. Quick flip - relisted within 3 days with price increase
        if (DaysBetweenListings <= 3 && PriceChangePercent > 0)
        {
            reasons.Add($"Quick flip: relisted in {DaysBetweenListings} days with {PriceChangePercent:F1}% price increase");
        }

        // 2. Price manipulation - significant price changes
        if (Math.Abs(PriceChangePercent ?? 0) > 20)
        {
            reasons.Add($"Significant price change: {PriceChangePercent:F1}%");
        }

        // 3. Stale listing reset - long time on market followed by quick relist
        if (PreviousDaysOnMarket > 60 && DaysBetweenListings <= 7)
        {
            reasons.Add($"Days-on-market reset: {PreviousDaysOnMarket} days, relisted in {DaysBetweenListings} days");
        }

        // 4. Frequent relisting without sale
        if (Type == RelistingType.VinMatch && DaysBetweenListings <= 14)
        {
            reasons.Add("Frequent relisting within 14 days (same VIN)");
        }

        IsSuspiciousPattern = reasons.Count > 0;
        SuspiciousReason = reasons.Count > 0 ? string.Join("; ", reasons) : null;
    }

    public void UpdateType(RelistingType type)
    {
        Type = type;
    }

    public void MarkAsSuspicious(string reason)
    {
        IsSuspiciousPattern = true;
        SuspiciousReason = string.IsNullOrEmpty(SuspiciousReason)
            ? reason
            : $"{SuspiciousReason}; {reason}";
    }

    public void ClearSuspiciousFlag()
    {
        IsSuspiciousPattern = false;
        SuspiciousReason = null;
    }
}

/// <summary>
/// Types of relisting patterns detected.
/// </summary>
public enum RelistingType
{
    /// <summary>Matched by VIN - highest confidence</summary>
    VinMatch,

    /// <summary>Matched by same external ID on same source site</summary>
    ExternalIdMatch,

    /// <summary>Matched by fuzzy matching on vehicle attributes</summary>
    FuzzyMatch,

    /// <summary>Matched by image similarity</summary>
    ImageMatch,

    /// <summary>Matched by multiple methods combined</summary>
    CombinedMatch
}
