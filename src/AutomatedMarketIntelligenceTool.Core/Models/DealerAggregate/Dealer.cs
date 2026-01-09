using AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

public class Dealer
{
    public DealerId DealerId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Phone { get; private set; }
    public int ListingCount { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    // Phase 5 Analytics: Reliability metrics (denormalized for quick access)
    public decimal? ReliabilityScore { get; private set; }
    public int? AvgDaysOnMarket { get; private set; }
    public int TotalListingsHistorical { get; private set; }
    public bool FrequentRelisterFlag { get; private set; }
    public DateTime? LastAnalyzedAt { get; private set; }

    // Navigation property to full metrics
    public DealerMetrics? DealerMetrics { get; private set; }

    private Dealer() { }

    public static Dealer Create(
        Guid tenantId,
        string name,
        string? city = null,
        string? state = null,
        string? phone = null)
    {
        return new Dealer
        {
            DealerId = DealerId.CreateNew(),
            TenantId = tenantId,
            Name = name,
            NormalizedName = NormalizeDealerName(name),
            City = city,
            State = state,
            Phone = phone,
            ListingCount = 0,
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
    }

    public void IncrementListingCount()
    {
        ListingCount++;
        LastSeenAt = DateTime.UtcNow;
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
    }

    public void UpdateContactInfo(string? city, string? state, string? phone)
    {
        City = city ?? City;
        State = state ?? State;
        Phone = phone ?? Phone;
    }

    public static string NormalizeDealerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove common suffixes and normalize
        var normalized = name.ToUpperInvariant()
            .Replace("INC.", "")
            .Replace("INC", "")
            .Replace("LLC", "")
            .Replace("L.L.C.", "")
            .Replace("MOTORS", "")
            .Replace("AUTOMOTIVE", "")
            .Replace("AUTO SALES", "")
            .Replace("  ", " ")
            .Trim();

        return normalized;
    }

    /// <summary>
    /// Updates the denormalized reliability metrics from DealerMetrics aggregate.
    /// </summary>
    public void UpdateReliabilityMetrics(decimal reliabilityScore, int avgDaysOnMarket,
        int totalListingsHistorical, bool frequentRelisterFlag)
    {
        ReliabilityScore = reliabilityScore;
        AvgDaysOnMarket = avgDaysOnMarket;
        TotalListingsHistorical = totalListingsHistorical;
        FrequentRelisterFlag = frequentRelisterFlag;
        LastAnalyzedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the frequent relister flag for dealers with suspicious relisting patterns.
    /// </summary>
    public void SetFrequentRelisterFlag(bool isFrequentRelister)
    {
        FrequentRelisterFlag = isFrequentRelister;
        LastAnalyzedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the historical listing count.
    /// </summary>
    public void IncrementHistoricalListings()
    {
        TotalListingsHistorical++;
    }
}
