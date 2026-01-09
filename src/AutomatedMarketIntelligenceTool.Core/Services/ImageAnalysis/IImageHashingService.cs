using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

/// <summary>
/// Service for computing and comparing image hashes for duplicate detection.
/// </summary>
public interface IImageHashingService
{
    /// <summary>
    /// Computes perceptual hashes for the given image URLs.
    /// </summary>
    /// <param name="imageUrls">URLs of images to hash.</param>
    /// <param name="maxImages">Maximum number of images to process (default 3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of computed hashes.</returns>
    Task<ImageHashResult> ComputeHashesAsync(
        IEnumerable<string> imageUrls,
        int maxImages = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds image-based matches among candidate listings.
    /// </summary>
    /// <param name="imageUrls">Image URLs of the listing to match.</param>
    /// <param name="candidates">Candidate listings to compare against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best matching listing, if any.</returns>
    Task<ImageMatchResult> FindImageMatchesAsync(
        IEnumerable<string> imageUrls,
        IEnumerable<Listing> candidates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two sets of image hashes and returns similarity information.
    /// </summary>
    /// <param name="hashes1">First set of hashes.</param>
    /// <param name="hashes2">Second set of hashes.</param>
    /// <returns>Comparison result with similarity metrics.</returns>
    ImageComparisonResult CompareHashes(IEnumerable<ulong> hashes1, IEnumerable<ulong> hashes2);
}

/// <summary>
/// Result of computing image hashes.
/// </summary>
public class ImageHashResult
{
    /// <summary>
    /// The computed hashes.
    /// </summary>
    public IReadOnlyList<ulong> Hashes { get; init; } = Array.Empty<ulong>();

    /// <summary>
    /// Number of images that were successfully processed.
    /// </summary>
    public int SuccessfulCount { get; init; }

    /// <summary>
    /// Number of images that failed to process.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Whether any hashes were computed.
    /// </summary>
    public bool HasHashes => Hashes.Count > 0;

    /// <summary>
    /// Creates an empty result (no images processed).
    /// </summary>
    public static ImageHashResult Empty() => new()
    {
        Hashes = Array.Empty<ulong>(),
        SuccessfulCount = 0,
        FailedCount = 0
    };
}

/// <summary>
/// Result of finding image matches.
/// </summary>
public class ImageMatchResult
{
    /// <summary>
    /// Whether a match was found.
    /// </summary>
    public bool HasMatch { get; init; }

    /// <summary>
    /// The matched listing (null if no match).
    /// </summary>
    public Listing? MatchedListing { get; init; }

    /// <summary>
    /// Number of images that matched.
    /// </summary>
    public int MatchingImageCount { get; init; }

    /// <summary>
    /// Total number of images compared.
    /// </summary>
    public int TotalImageCount { get; init; }

    /// <summary>
    /// Overall similarity score (0-100).
    /// </summary>
    public double SimilarityScore { get; init; }

    /// <summary>
    /// Creates a result indicating no images were available.
    /// </summary>
    public static ImageMatchResult NoImages() => new()
    {
        HasMatch = false,
        MatchingImageCount = 0,
        TotalImageCount = 0,
        SimilarityScore = 0
    };

    /// <summary>
    /// Creates a result indicating no match was found.
    /// </summary>
    public static ImageMatchResult NoMatch(int totalImages = 0) => new()
    {
        HasMatch = false,
        MatchingImageCount = 0,
        TotalImageCount = totalImages,
        SimilarityScore = 0
    };

    /// <summary>
    /// Creates a result indicating a match was found.
    /// </summary>
    public static ImageMatchResult Match(Listing listing, int matchingCount, int totalCount, double similarity) => new()
    {
        HasMatch = true,
        MatchedListing = listing,
        MatchingImageCount = matchingCount,
        TotalImageCount = totalCount,
        SimilarityScore = similarity
    };
}

/// <summary>
/// Result of comparing two sets of image hashes.
/// </summary>
public class ImageComparisonResult
{
    /// <summary>
    /// Number of images that matched.
    /// </summary>
    public int MatchingCount { get; init; }

    /// <summary>
    /// Total number of images in the first set.
    /// </summary>
    public int FirstSetCount { get; init; }

    /// <summary>
    /// Total number of images in the second set.
    /// </summary>
    public int SecondSetCount { get; init; }

    /// <summary>
    /// Whether a majority of images matched.
    /// </summary>
    public bool IsMajorityMatch { get; init; }

    /// <summary>
    /// Average similarity percentage across all comparisons.
    /// </summary>
    public double AverageSimilarity { get; init; }

    /// <summary>
    /// Best single image match similarity percentage.
    /// </summary>
    public double BestMatchSimilarity { get; init; }
}
