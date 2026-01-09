using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Deduplication;

/// <summary>
/// Service for processing batch deduplication operations with optimized
/// blocking/bucketing strategies for improved performance.
/// </summary>
public interface IBatchDeduplicationService
{
    /// <summary>
    /// Processes a batch of scraped listings for deduplication.
    /// Uses blocking/bucketing strategy to reduce comparison overhead.
    /// </summary>
    /// <param name="listings">The listings to process.</param>
    /// <param name="tenantId">The tenant ID for the operation.</param>
    /// <param name="options">Deduplication options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch deduplication result with statistics.</returns>
    Task<BatchDeduplicationResult> ProcessBatchAsync(
        IEnumerable<ScrapedListing> listings,
        Guid tenantId,
        DuplicateDetectionOptions? options = null,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pre-fetches candidate blocks for efficient batch processing.
    /// </summary>
    /// <param name="listings">The listings to prepare candidates for.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Candidate block index for efficient lookup.</returns>
    Task<CandidateBlockIndex> PreFetchCandidateBlocksAsync(
        IEnumerable<ScrapedListing> listings,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a batch deduplication operation.
/// </summary>
public class BatchDeduplicationResult
{
    /// <summary>
    /// Total number of listings processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of new listings identified.
    /// </summary>
    public int NewListingCount { get; set; }

    /// <summary>
    /// Number of duplicate listings found.
    /// </summary>
    public int DuplicateCount { get; set; }

    /// <summary>
    /// Number of near-matches flagged for review.
    /// </summary>
    public int NearMatchCount { get; set; }

    /// <summary>
    /// Number of VIN matches.
    /// </summary>
    public int VinMatchCount { get; set; }

    /// <summary>
    /// Number of fuzzy matches.
    /// </summary>
    public int FuzzyMatchCount { get; set; }

    /// <summary>
    /// Number of image matches.
    /// </summary>
    public int ImageMatchCount { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Average processing time per listing in milliseconds.
    /// </summary>
    public double AverageTimePerListingMs => TotalProcessed > 0
        ? (double)ProcessingTimeMs / TotalProcessed
        : 0;

    /// <summary>
    /// Individual results for each listing.
    /// </summary>
    public List<BatchItemResult> ItemResults { get; set; } = new();

    /// <summary>
    /// Errors encountered during processing.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result for a single listing in batch processing.
/// </summary>
public class BatchItemResult
{
    /// <summary>
    /// External ID of the listing.
    /// </summary>
    public required string ExternalId { get; init; }

    /// <summary>
    /// Source site of the listing.
    /// </summary>
    public required string SourceSite { get; init; }

    /// <summary>
    /// The deduplication check result.
    /// </summary>
    public required DuplicateCheckResult Result { get; init; }

    /// <summary>
    /// Processing time for this item in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; init; }
}

/// <summary>
/// Progress information for batch operations.
/// </summary>
public class BatchProgress
{
    /// <summary>
    /// Number of items processed so far.
    /// </summary>
    public int Processed { get; }

    /// <summary>
    /// Total number of items to process.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Percentage complete (0-100).
    /// </summary>
    public double PercentComplete => Total > 0 ? (double)Processed / Total * 100 : 0;

    /// <summary>
    /// Current status message.
    /// </summary>
    public string? Message { get; init; }

    public BatchProgress(int processed, int total)
    {
        Processed = processed;
        Total = total;
    }
}

/// <summary>
/// Index of candidate listings organized by blocking key for efficient lookup.
/// </summary>
public class CandidateBlockIndex
{
    private readonly Dictionary<string, List<CandidateListing>> _blocks = new();

    public CandidateBlockIndex()
    {
    }

    public CandidateBlockIndex(IEnumerable<CandidateListing> candidates)
    {
        foreach (var candidate in candidates)
        {
            var key = GetBlockKey(candidate.Make, candidate.Year);
            if (!_blocks.TryGetValue(key, out var block))
            {
                block = new List<CandidateListing>();
                _blocks[key] = block;
            }
            block.Add(candidate);
        }
    }

    /// <summary>
    /// Gets candidates for the specified make and year range.
    /// </summary>
    public IEnumerable<CandidateListing> GetCandidates(string? make, int? year)
    {
        if (string.IsNullOrEmpty(make))
        {
            return Enumerable.Empty<CandidateListing>();
        }

        var results = new List<CandidateListing>();
        var yearRange = year.HasValue
            ? Enumerable.Range(year.Value - 2, 5)
            : Enumerable.Range(DateTime.Now.Year - 20, 25);

        foreach (var y in yearRange)
        {
            var key = GetBlockKey(make, y);
            if (_blocks.TryGetValue(key, out var block))
            {
                results.AddRange(block);
            }
        }

        return results.Distinct();
    }

    /// <summary>
    /// Adds a candidate to the index.
    /// </summary>
    public void AddCandidate(CandidateListing candidate)
    {
        var key = GetBlockKey(candidate.Make, candidate.Year);
        if (!_blocks.TryGetValue(key, out var block))
        {
            block = new List<CandidateListing>();
            _blocks[key] = block;
        }
        block.Add(candidate);
    }

    /// <summary>
    /// Total number of candidates in the index.
    /// </summary>
    public int TotalCandidates => _blocks.Values.Sum(b => b.Count);

    /// <summary>
    /// Number of blocks in the index.
    /// </summary>
    public int BlockCount => _blocks.Count;

    private static string GetBlockKey(string? make, int? year)
    {
        var normalizedMake = (make ?? "unknown").ToUpperInvariant().Trim();
        var normalizedYear = year ?? 0;
        return $"{normalizedMake}:{normalizedYear}";
    }
}

/// <summary>
/// Lightweight representation of a listing for candidate matching.
/// </summary>
public class CandidateListing
{
    public Guid ListingId { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int Year { get; init; }
    public decimal Price { get; init; }
    public int? Mileage { get; init; }
    public string? Vin { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? ExternalId { get; init; }
    public string? SourceSite { get; init; }
    public Guid? LinkedVehicleId { get; init; }
}
