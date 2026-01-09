using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for managing the review queue for near-match duplicates.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Creates a new review item for a near-match pair.
    /// </summary>
    Task<ReviewItem> CreateReviewItemAsync(
        Guid tenantId,
        ListingId listing1Id,
        ListingId listing2Id,
        double confidenceScore,
        MatchMethod matchMethod,
        FuzzyMatchDetails? fieldScores = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending review items for a tenant.
    /// </summary>
    Task<IReadOnlyList<ReviewItemInfo>> GetPendingReviewsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets review items with filtering options.
    /// </summary>
    Task<ReviewListResult> GetReviewsAsync(
        Guid tenantId,
        ReviewFilterOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific review item by ID.
    /// </summary>
    Task<ReviewItemInfo?> GetReviewByIdAsync(
        Guid tenantId,
        ReviewItemId reviewItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a review item with the given decision.
    /// </summary>
    Task<bool> ResolveReviewAsync(
        Guid tenantId,
        ReviewItemId reviewItemId,
        ResolutionDecision decision,
        string? resolvedBy = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a review item without resolution.
    /// </summary>
    Task<bool> DismissReviewAsync(
        Guid tenantId,
        ReviewItemId reviewItemId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a review already exists for the given listing pair.
    /// </summary>
    Task<bool> ReviewExistsAsync(
        Guid tenantId,
        ListingId listing1Id,
        ListingId listing2Id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the review queue.
    /// </summary>
    Task<ReviewQueueStats> GetStatsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Review item with associated listing information.
/// </summary>
public class ReviewItemInfo
{
    public ReviewItemId ReviewItemId { get; init; } = null!;
    public decimal ConfidenceScore { get; init; }
    public MatchMethod MatchMethod { get; init; }
    public ReviewItemStatus Status { get; init; }
    public ResolutionDecision Resolution { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolvedBy { get; init; }
    public string? Notes { get; init; }

    // Listing 1 details
    public ListingId Listing1Id { get; init; } = null!;
    public string Listing1Make { get; init; } = string.Empty;
    public string Listing1Model { get; init; } = string.Empty;
    public int Listing1Year { get; init; }
    public decimal Listing1Price { get; init; }
    public int? Listing1Mileage { get; init; }
    public string? Listing1City { get; init; }
    public string Listing1SourceSite { get; init; } = string.Empty;

    // Listing 2 details
    public ListingId Listing2Id { get; init; } = null!;
    public string Listing2Make { get; init; } = string.Empty;
    public string Listing2Model { get; init; } = string.Empty;
    public int Listing2Year { get; init; }
    public decimal Listing2Price { get; init; }
    public int? Listing2Mileage { get; init; }
    public string? Listing2City { get; init; }
    public string Listing2SourceSite { get; init; } = string.Empty;

    // Field scores (if available)
    public FuzzyMatchDetails? FieldScores { get; init; }
}

/// <summary>
/// Filter options for querying reviews.
/// </summary>
public class ReviewFilterOptions
{
    public ReviewItemStatus? Status { get; init; }
    public MatchMethod? MatchMethod { get; init; }
    public double? MinConfidence { get; init; }
    public double? MaxConfidence { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public static ReviewFilterOptions Pending => new() { Status = ReviewItemStatus.Pending };
}

/// <summary>
/// Paginated result of review items.
/// </summary>
public class ReviewListResult
{
    public IReadOnlyList<ReviewItemInfo> Items { get; init; } = Array.Empty<ReviewItemInfo>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Statistics about the review queue.
/// </summary>
public class ReviewQueueStats
{
    public int TotalCount { get; init; }
    public int PendingCount { get; init; }
    public int ResolvedCount { get; init; }
    public int DismissedCount { get; init; }
    public int SameVehicleCount { get; init; }
    public int DifferentVehicleCount { get; init; }
    public double AverageConfidenceScore { get; init; }
}
