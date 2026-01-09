using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;

/// <summary>
/// Represents the reliability and performance metrics for a dealer.
/// This is used for dealer analytics, tracking historical performance,
/// and flagging potential issues like frequent relisting.
/// </summary>
public class DealerMetrics
{
    public DealerMetricsId DealerMetricsId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public DealerId DealerId { get; private set; } = null!;

    // Core reliability metrics (0-100 scale)
    public decimal ReliabilityScore { get; private set; }

    // Listing metrics
    public int TotalListingsHistorical { get; private set; }
    public int ActiveListingsCount { get; private set; }
    public int SoldListingsCount { get; private set; }
    public int ExpiredListingsCount { get; private set; }

    // Time-based metrics
    public double AverageDaysOnMarket { get; private set; }
    public double MedianDaysOnMarket { get; private set; }
    public int MinDaysOnMarket { get; private set; }
    public int MaxDaysOnMarket { get; private set; }

    // Pricing metrics
    public decimal AverageListingPrice { get; private set; }
    public decimal AveragePriceReduction { get; private set; }
    public double AveragePriceReductionPercent { get; private set; }
    public int PriceReductionCount { get; private set; }

    // Relisting metrics
    public int RelistingCount { get; private set; }
    public double RelistingRate { get; private set; }
    public bool IsFrequentRelister { get; private set; }

    // Data quality metrics
    public double VinProvidedRate { get; private set; }
    public double ImageProvidedRate { get; private set; }
    public double DescriptionQualityScore { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime LastAnalyzedAt { get; private set; }
    public DateTime? NextScheduledAnalysis { get; private set; }

    private DealerMetrics() { }

    public static DealerMetrics Create(Guid tenantId, DealerId dealerId)
    {
        return new DealerMetrics
        {
            DealerMetricsId = DealerMetricsId.CreateNew(),
            TenantId = tenantId,
            DealerId = dealerId,
            ReliabilityScore = 50m, // Start with neutral score
            TotalListingsHistorical = 0,
            ActiveListingsCount = 0,
            SoldListingsCount = 0,
            ExpiredListingsCount = 0,
            AverageDaysOnMarket = 0,
            MedianDaysOnMarket = 0,
            MinDaysOnMarket = 0,
            MaxDaysOnMarket = 0,
            AverageListingPrice = 0,
            AveragePriceReduction = 0,
            AveragePriceReductionPercent = 0,
            PriceReductionCount = 0,
            RelistingCount = 0,
            RelistingRate = 0,
            IsFrequentRelister = false,
            VinProvidedRate = 0,
            ImageProvidedRate = 0,
            DescriptionQualityScore = 0,
            CreatedAt = DateTime.UtcNow,
            LastAnalyzedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the dealer metrics with new analysis results.
    /// </summary>
    public void UpdateMetrics(DealerMetricsUpdateData data)
    {
        TotalListingsHistorical = data.TotalListingsHistorical;
        ActiveListingsCount = data.ActiveListingsCount;
        SoldListingsCount = data.SoldListingsCount;
        ExpiredListingsCount = data.ExpiredListingsCount;

        AverageDaysOnMarket = data.AverageDaysOnMarket;
        MedianDaysOnMarket = data.MedianDaysOnMarket;
        MinDaysOnMarket = data.MinDaysOnMarket;
        MaxDaysOnMarket = data.MaxDaysOnMarket;

        AverageListingPrice = data.AverageListingPrice;
        AveragePriceReduction = data.AveragePriceReduction;
        AveragePriceReductionPercent = data.AveragePriceReductionPercent;
        PriceReductionCount = data.PriceReductionCount;

        RelistingCount = data.RelistingCount;
        RelistingRate = data.RelistingRate;

        VinProvidedRate = data.VinProvidedRate;
        ImageProvidedRate = data.ImageProvidedRate;
        DescriptionQualityScore = data.DescriptionQualityScore;

        LastAnalyzedAt = DateTime.UtcNow;

        // Recalculate reliability score and relister status
        RecalculateReliabilityScore();
        UpdateFrequentRelisterStatus();
    }

    /// <summary>
    /// Recalculates the overall reliability score based on multiple factors.
    /// Score ranges from 0 to 100.
    /// </summary>
    private void RecalculateReliabilityScore()
    {
        // Component weights
        const double DataQualityWeight = 0.25;
        const double RelistingWeight = 0.20;
        const double TimeOnMarketWeight = 0.20;
        const double VolumeWeight = 0.15;
        const double PricingWeight = 0.20;

        // Data quality component (0-100)
        var dataQualityScore =
            (VinProvidedRate * 40) +
            (ImageProvidedRate * 30) +
            (DescriptionQualityScore * 30);

        // Relisting component (100 = no relisting, 0 = frequent relisting)
        var relistingScore = Math.Max(0, 100 - (RelistingRate * 200));

        // Time on market component (faster selling = higher score)
        // 30 days = excellent, 90 days = average, 180+ days = poor
        var timeScore = AverageDaysOnMarket switch
        {
            <= 30 => 100.0,
            <= 60 => 80.0,
            <= 90 => 60.0,
            <= 120 => 40.0,
            <= 180 => 20.0,
            _ => 10.0
        };

        // Volume component (established dealers get bonus)
        var volumeScore = TotalListingsHistorical switch
        {
            >= 100 => 100.0,
            >= 50 => 80.0,
            >= 20 => 60.0,
            >= 10 => 40.0,
            >= 5 => 30.0,
            _ => 20.0
        };

        // Pricing component (reasonable price reductions = good)
        var pricingScore = AveragePriceReductionPercent switch
        {
            <= 5 => 100.0,
            <= 10 => 80.0,
            <= 15 => 60.0,
            <= 20 => 40.0,
            <= 30 => 20.0,
            _ => 10.0
        };

        ReliabilityScore = (decimal)(
            (dataQualityScore * DataQualityWeight) +
            (relistingScore * RelistingWeight) +
            (timeScore * TimeOnMarketWeight) +
            (volumeScore * VolumeWeight) +
            (pricingScore * PricingWeight)
        );

        // Clamp to valid range
        ReliabilityScore = Math.Max(0, Math.Min(100, ReliabilityScore));
    }

    /// <summary>
    /// Determines if dealer should be flagged as a frequent relister.
    /// Threshold: More than 20% relisting rate OR more than 5 relisted items.
    /// </summary>
    private void UpdateFrequentRelisterStatus()
    {
        const double RelistingRateThreshold = 0.20;
        const int MinRelistingCountThreshold = 5;

        IsFrequentRelister =
            (RelistingRate >= RelistingRateThreshold && RelistingCount >= MinRelistingCountThreshold) ||
            RelistingCount >= 10;
    }

    public void ScheduleNextAnalysis(TimeSpan interval)
    {
        NextScheduledAnalysis = DateTime.UtcNow.Add(interval);
    }

    public void IncrementActiveListings()
    {
        ActiveListingsCount++;
        TotalListingsHistorical++;
    }

    public void DecrementActiveListings()
    {
        if (ActiveListingsCount > 0)
            ActiveListingsCount--;
    }

    public void IncrementSoldListings()
    {
        SoldListingsCount++;
        DecrementActiveListings();
    }

    public void IncrementExpiredListings()
    {
        ExpiredListingsCount++;
        DecrementActiveListings();
    }

    public void IncrementRelistingCount()
    {
        RelistingCount++;
        if (TotalListingsHistorical > 0)
        {
            RelistingRate = (double)RelistingCount / TotalListingsHistorical;
        }
        UpdateFrequentRelisterStatus();
    }
}

/// <summary>
/// Data transfer object for updating dealer metrics.
/// </summary>
public class DealerMetricsUpdateData
{
    public int TotalListingsHistorical { get; init; }
    public int ActiveListingsCount { get; init; }
    public int SoldListingsCount { get; init; }
    public int ExpiredListingsCount { get; init; }

    public double AverageDaysOnMarket { get; init; }
    public double MedianDaysOnMarket { get; init; }
    public int MinDaysOnMarket { get; init; }
    public int MaxDaysOnMarket { get; init; }

    public decimal AverageListingPrice { get; init; }
    public decimal AveragePriceReduction { get; init; }
    public double AveragePriceReductionPercent { get; init; }
    public int PriceReductionCount { get; init; }

    public int RelistingCount { get; init; }
    public double RelistingRate { get; init; }

    public double VinProvidedRate { get; init; }
    public double ImageProvidedRate { get; init; }
    public double DescriptionQualityScore { get; init; }
}
