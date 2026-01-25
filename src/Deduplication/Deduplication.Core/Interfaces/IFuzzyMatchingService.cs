using Deduplication.Core.Entities;
using Deduplication.Core.Models;

namespace Deduplication.Core.Interfaces;

/// <summary>
/// Service for fuzzy matching between listings.
/// </summary>
public interface IFuzzyMatchingService
{
    MatchResult CalculateMatchScore(
        ListingData source,
        ListingData target,
        DeduplicationConfig config);

    Task<IReadOnlyList<MatchResult>> FindPotentialMatchesAsync(
        ListingData listing,
        IEnumerable<ListingData> candidates,
        DeduplicationConfig config,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a fuzzy match calculation.
/// </summary>
public sealed record MatchResult
{
    public Guid SourceId { get; init; }
    public Guid TargetId { get; init; }
    public double OverallScore { get; init; }
    public double VinScore { get; init; }
    public double TitleScore { get; init; }
    public double PriceScore { get; init; }
    public double LocationScore { get; init; }
    public double ImageScore { get; init; }
    public bool IsAboveThreshold { get; init; }
    public bool RequiresReview { get; init; }
}
