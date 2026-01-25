namespace Deduplication.Core.Entities;

/// <summary>
/// Configuration for deduplication matching.
/// </summary>
public sealed class DeduplicationConfig
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = "Default";
    public bool IsActive { get; private set; } = true;

    // Threshold settings
    public double TitleSimilarityThreshold { get; private set; } = 0.85;
    public double ImageHashSimilarityThreshold { get; private set; } = 0.90;
    public double OverallMatchThreshold { get; private set; } = 0.80;
    public double ReviewThreshold { get; private set; } = 0.70;

    // Weight settings for scoring
    public double TitleWeight { get; private set; } = 0.25;
    public double VinWeight { get; private set; } = 0.30;
    public double ImageHashWeight { get; private set; } = 0.20;
    public double PriceWeight { get; private set; } = 0.10;
    public double MileageWeight { get; private set; } = 0.10;
    public double LocationWeight { get; private set; } = 0.05;

    // Matching rules
    public bool RequireVinMatch { get; private set; } = false;
    public bool RequireImageMatch { get; private set; } = false;
    public int PriceTolerancePercent { get; private set; } = 5;
    public int MileageTolerancePercent { get; private set; } = 3;
    public int MaxDistanceKm { get; private set; } = 100;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private DeduplicationConfig() { }

    public static DeduplicationConfig CreateDefault()
    {
        return new DeduplicationConfig
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateThresholds(
        double? titleThreshold = null,
        double? imageThreshold = null,
        double? overallThreshold = null,
        double? reviewThreshold = null)
    {
        if (titleThreshold.HasValue) TitleSimilarityThreshold = titleThreshold.Value;
        if (imageThreshold.HasValue) ImageHashSimilarityThreshold = imageThreshold.Value;
        if (overallThreshold.HasValue) OverallMatchThreshold = overallThreshold.Value;
        if (reviewThreshold.HasValue) ReviewThreshold = reviewThreshold.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateWeights(
        double? titleWeight = null,
        double? vinWeight = null,
        double? imageWeight = null,
        double? priceWeight = null,
        double? mileageWeight = null,
        double? locationWeight = null)
    {
        if (titleWeight.HasValue) TitleWeight = titleWeight.Value;
        if (vinWeight.HasValue) VinWeight = vinWeight.Value;
        if (imageWeight.HasValue) ImageHashWeight = imageWeight.Value;
        if (priceWeight.HasValue) PriceWeight = priceWeight.Value;
        if (mileageWeight.HasValue) MileageWeight = mileageWeight.Value;
        if (locationWeight.HasValue) LocationWeight = locationWeight.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
