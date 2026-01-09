using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Implementation of the review service for managing near-match reviews.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<ReviewService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReviewItem> CreateReviewItemAsync(
        Guid tenantId,
        ListingId listing1Id,
        ListingId listing2Id,
        double confidenceScore,
        MatchMethod matchMethod,
        FuzzyMatchDetails? fieldScores = null,
        CancellationToken cancellationToken = default)
    {
        // Check if review already exists
        var exists = await ReviewExistsAsync(tenantId, listing1Id, listing2Id, cancellationToken);
        if (exists)
        {
            _logger.LogDebug("Review already exists for listings {Listing1Id} and {Listing2Id}",
                listing1Id.Value, listing2Id.Value);
            throw new InvalidOperationException("A review already exists for this listing pair");
        }

        var fieldScoresJson = fieldScores != null
            ? JsonSerializer.Serialize(fieldScores)
            : null;

        var reviewItem = ReviewItem.Create(
            tenantId,
            listing1Id,
            listing2Id,
            (decimal)confidenceScore,
            matchMethod,
            fieldScoresJson);

        _context.ReviewItems.Add(reviewItem);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created review item {ReviewItemId} for listings {Listing1Id} and {Listing2Id} with {Confidence:F1}% confidence",
            reviewItem.ReviewItemId.Value, listing1Id.Value, listing2Id.Value, confidenceScore);

        return reviewItem;
    }

    public async Task<IReadOnlyList<ReviewItemInfo>> GetPendingReviewsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await GetReviewsAsync(tenantId, ReviewFilterOptions.Pending, cancellationToken);
        return result.Items;
    }

    public async Task<ReviewListResult> GetReviewsAsync(
        Guid tenantId,
        ReviewFilterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ReviewFilterOptions();

        var query = _context.ReviewItems
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId);

        // Apply filters
        if (options.Status.HasValue)
        {
            query = query.Where(r => r.Status == options.Status.Value);
        }

        if (options.MatchMethod.HasValue)
        {
            query = query.Where(r => r.MatchMethod == options.MatchMethod.Value);
        }

        if (options.MinConfidence.HasValue)
        {
            query = query.Where(r => r.ConfidenceScore >= (decimal)options.MinConfidence.Value);
        }

        if (options.MaxConfidence.HasValue)
        {
            query = query.Where(r => r.ConfidenceScore <= (decimal)options.MaxConfidence.Value);
        }

        if (options.CreatedAfter.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= options.CreatedAfter.Value);
        }

        if (options.CreatedBefore.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= options.CreatedBefore.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var reviewItems = await query
            .OrderByDescending(r => r.ConfidenceScore)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        // Load associated listings
        var listingIds = reviewItems
            .SelectMany(r => new[] { r.Listing1Id.Value, r.Listing2Id.Value })
            .Distinct()
            .ToList();

        var listings = await _context.Listings
            .IgnoreQueryFilters()
            .Where(l => listingIds.Contains(l.ListingId.Value))
            .ToDictionaryAsync(l => l.ListingId.Value, cancellationToken);

        var items = reviewItems.Select(r => MapToReviewItemInfo(r, listings)).ToList();

        return new ReviewListResult
        {
            Items = items.AsReadOnly(),
            TotalCount = totalCount,
            Page = options.Page,
            PageSize = options.PageSize
        };
    }

    public async Task<ReviewItemInfo?> GetReviewByIdAsync(
        Guid tenantId,
        ReviewItemId reviewItemId,
        CancellationToken cancellationToken = default)
    {
        var reviewItem = await _context.ReviewItems
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .FirstOrDefaultAsync(r => r.ReviewItemId == reviewItemId, cancellationToken);

        if (reviewItem == null)
        {
            return null;
        }

        var listings = await _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.ListingId == reviewItem.Listing1Id || l.ListingId == reviewItem.Listing2Id)
            .ToDictionaryAsync(l => l.ListingId.Value, cancellationToken);

        return MapToReviewItemInfo(reviewItem, listings);
    }

    public async Task<bool> ResolveReviewAsync(
        Guid tenantId,
        ReviewItemId reviewItemId,
        ResolutionDecision decision,
        string? resolvedBy = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var reviewItem = await _context.ReviewItems
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .FirstOrDefaultAsync(r => r.ReviewItemId == reviewItemId, cancellationToken);

        if (reviewItem == null)
        {
            _logger.LogWarning("Review item {ReviewItemId} not found", reviewItemId.Value);
            return false;
        }

        try
        {
            reviewItem.Resolve(decision, resolvedBy, notes);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Resolved review item {ReviewItemId} as {Decision}",
                reviewItemId.Value, decision);

            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot resolve review item {ReviewItemId}", reviewItemId.Value);
            return false;
        }
    }

    public async Task<bool> DismissReviewAsync(
        Guid tenantId,
        ReviewItemId reviewItemId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var reviewItem = await _context.ReviewItems
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .FirstOrDefaultAsync(r => r.ReviewItemId == reviewItemId, cancellationToken);

        if (reviewItem == null)
        {
            _logger.LogWarning("Review item {ReviewItemId} not found", reviewItemId.Value);
            return false;
        }

        try
        {
            reviewItem.Dismiss(reason);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Dismissed review item {ReviewItemId}", reviewItemId.Value);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot dismiss review item {ReviewItemId}", reviewItemId.Value);
            return false;
        }
    }

    public async Task<bool> ReviewExistsAsync(
        Guid tenantId,
        ListingId listing1Id,
        ListingId listing2Id,
        CancellationToken cancellationToken = default)
    {
        // Check both orderings of the listing pair
        return await _context.ReviewItems
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Where(r => r.Status == ReviewItemStatus.Pending)
            .AnyAsync(r =>
                (r.Listing1Id == listing1Id && r.Listing2Id == listing2Id) ||
                (r.Listing1Id == listing2Id && r.Listing2Id == listing1Id),
                cancellationToken);
    }

    public async Task<ReviewQueueStats> GetStatsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _context.ReviewItems
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return new ReviewQueueStats
        {
            TotalCount = reviews.Count,
            PendingCount = reviews.Count(r => r.Status == ReviewItemStatus.Pending),
            ResolvedCount = reviews.Count(r => r.Status == ReviewItemStatus.Resolved),
            DismissedCount = reviews.Count(r => r.Status == ReviewItemStatus.Dismissed),
            SameVehicleCount = reviews.Count(r => r.Resolution == ResolutionDecision.SameVehicle),
            DifferentVehicleCount = reviews.Count(r => r.Resolution == ResolutionDecision.DifferentVehicle),
            AverageConfidenceScore = reviews.Count > 0 ? (double)reviews.Average(r => r.ConfidenceScore) : 0
        };
    }

    private static ReviewItemInfo MapToReviewItemInfo(
        ReviewItem reviewItem,
        Dictionary<Guid, Listing> listings)
    {
        listings.TryGetValue(reviewItem.Listing1Id.Value, out var listing1);
        listings.TryGetValue(reviewItem.Listing2Id.Value, out var listing2);

        FuzzyMatchDetails? fieldScores = null;
        if (!string.IsNullOrEmpty(reviewItem.FieldScores))
        {
            try
            {
                fieldScores = JsonSerializer.Deserialize<FuzzyMatchDetails>(reviewItem.FieldScores);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new ReviewItemInfo
        {
            ReviewItemId = reviewItem.ReviewItemId,
            ConfidenceScore = reviewItem.ConfidenceScore,
            MatchMethod = reviewItem.MatchMethod,
            Status = reviewItem.Status,
            Resolution = reviewItem.Resolution,
            CreatedAt = reviewItem.CreatedAt,
            ResolvedAt = reviewItem.ResolvedAt,
            ResolvedBy = reviewItem.ResolvedBy,
            Notes = reviewItem.Notes,
            FieldScores = fieldScores,

            Listing1Id = reviewItem.Listing1Id,
            Listing1Make = listing1?.Make ?? "Unknown",
            Listing1Model = listing1?.Model ?? "Unknown",
            Listing1Year = listing1?.Year ?? 0,
            Listing1Price = listing1?.Price ?? 0,
            Listing1Mileage = listing1?.Mileage,
            Listing1City = listing1?.City,
            Listing1SourceSite = listing1?.SourceSite ?? "Unknown",

            Listing2Id = reviewItem.Listing2Id,
            Listing2Make = listing2?.Make ?? "Unknown",
            Listing2Model = listing2?.Model ?? "Unknown",
            Listing2Year = listing2?.Year ?? 0,
            Listing2Price = listing2?.Price ?? 0,
            Listing2Mileage = listing2?.Mileage,
            Listing2City = listing2?.City,
            Listing2SourceSite = listing2?.SourceSite ?? "Unknown"
        };
    }
}
