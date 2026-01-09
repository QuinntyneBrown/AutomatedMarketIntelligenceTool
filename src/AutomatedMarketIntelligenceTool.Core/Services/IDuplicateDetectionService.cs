namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IDuplicateDetectionService
{
    Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default);

    Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        DuplicateDetectionOptions options,
        CancellationToken cancellationToken = default);
}

public class ScrapedListingInfo
{
    public required string ExternalId { get; init; }
    public required string SourceSite { get; init; }
    public string? Vin { get; init; }
    public required Guid TenantId { get; init; }

    // Fuzzy matching fields
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int? Mileage { get; init; }
    public decimal? Price { get; init; }
    public string? City { get; init; }

    // Image matching fields
    public IEnumerable<string>? ImageUrls { get; init; }
}

public class DuplicateDetectionOptions
{
    /// <summary>
    /// Enable fuzzy matching when VIN and ExternalId don't match.
    /// </summary>
    public bool EnableFuzzyMatching { get; init; } = true;

    /// <summary>
    /// Enable image-based matching.
    /// </summary>
    public bool EnableImageMatching { get; init; } = false;

    /// <summary>
    /// Confidence threshold for automatic deduplication (0-100). Default 85%.
    /// </summary>
    public double AutoMatchThreshold { get; init; } = 85.0;

    /// <summary>
    /// Lower threshold for near-matches to be flagged for review (0-100). Default 60%.
    /// </summary>
    public double ReviewThreshold { get; init; } = 60.0;

    /// <summary>
    /// Mileage tolerance for fuzzy matching (+/- miles). Default 500.
    /// </summary>
    public int MileageTolerance { get; init; } = 500;

    /// <summary>
    /// Price tolerance for fuzzy matching (+/- dollars). Default 500.
    /// </summary>
    public decimal PriceTolerance { get; init; } = 500;

    /// <summary>
    /// Default options for duplicate detection.
    /// </summary>
    public static DuplicateDetectionOptions Default => new();

    /// <summary>
    /// Options with image matching enabled.
    /// </summary>
    public static DuplicateDetectionOptions WithImageMatching => new()
    {
        EnableImageMatching = true
    };
}

public class DuplicateCheckResult
{
    public bool IsDuplicate { get; init; }
    public DuplicateMatchType MatchType { get; init; }
    public Guid? ExistingListingId { get; init; }

    /// <summary>
    /// Confidence score for fuzzy/image matches (0-100).
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// Whether this is a near-match that should be flagged for review.
    /// </summary>
    public bool RequiresReview { get; init; }

    /// <summary>
    /// Detailed field-level match scores for fuzzy matching.
    /// </summary>
    public FuzzyMatchDetails? FuzzyMatchDetails { get; init; }

    public static DuplicateCheckResult VinMatch(Guid existingListingId) =>
        new()
        {
            IsDuplicate = true,
            MatchType = DuplicateMatchType.VinMatch,
            ExistingListingId = existingListingId,
            ConfidenceScore = 100.0
        };

    public static DuplicateCheckResult ExternalIdMatch(Guid existingListingId) =>
        new()
        {
            IsDuplicate = true,
            MatchType = DuplicateMatchType.ExternalIdMatch,
            ExistingListingId = existingListingId,
            ConfidenceScore = 100.0
        };

    public static DuplicateCheckResult FuzzyMatch(
        Guid existingListingId,
        double confidence,
        FuzzyMatchDetails? details = null) =>
        new()
        {
            IsDuplicate = true,
            MatchType = DuplicateMatchType.FuzzyMatch,
            ExistingListingId = existingListingId,
            ConfidenceScore = confidence,
            FuzzyMatchDetails = details
        };

    public static DuplicateCheckResult ImageMatch(
        Guid existingListingId,
        double confidence) =>
        new()
        {
            IsDuplicate = true,
            MatchType = DuplicateMatchType.ImageMatch,
            ExistingListingId = existingListingId,
            ConfidenceScore = confidence
        };

    public static DuplicateCheckResult NearMatch(
        Guid existingListingId,
        double confidence,
        DuplicateMatchType matchType,
        FuzzyMatchDetails? details = null) =>
        new()
        {
            IsDuplicate = false,
            MatchType = matchType,
            ExistingListingId = existingListingId,
            ConfidenceScore = confidence,
            RequiresReview = true,
            FuzzyMatchDetails = details
        };

    public static DuplicateCheckResult NewListing() =>
        new()
        {
            IsDuplicate = false,
            MatchType = DuplicateMatchType.None,
            ExistingListingId = null,
            ConfidenceScore = 0
        };
}

public class FuzzyMatchDetails
{
    public double MakeModelScore { get; init; }
    public double YearScore { get; init; }
    public double MileageScore { get; init; }
    public double PriceScore { get; init; }
    public double LocationScore { get; init; }
    public double ImageScore { get; init; }
    public double OverallScore { get; init; }
}

public enum DuplicateMatchType
{
    None,
    VinMatch,
    ExternalIdMatch,
    FuzzyMatch,
    ImageMatch,
    CombinedMatch  // Fuzzy + Image
}
