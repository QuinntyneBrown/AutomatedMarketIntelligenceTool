using Deduplication.Core.Enums;

namespace Deduplication.Core.Entities;

/// <summary>
/// Represents a detected duplicate match between two listings.
/// </summary>
public sealed class DuplicateMatch
{
    public Guid Id { get; init; }
    public Guid SourceListingId { get; init; }
    public Guid TargetListingId { get; init; }
    public double OverallScore { get; init; }
    public MatchConfidence Confidence { get; init; }
    public MatchScoreBreakdown ScoreBreakdown { get; init; } = new();
    public DateTimeOffset DetectedAt { get; init; }
    public bool IsConfirmed { get; private set; }
    public Guid? ReviewItemId { get; private set; }

    private DuplicateMatch() { }

    public static DuplicateMatch Create(
        Guid sourceListingId,
        Guid targetListingId,
        double overallScore,
        MatchScoreBreakdown breakdown)
    {
        return new DuplicateMatch
        {
            Id = Guid.NewGuid(),
            SourceListingId = sourceListingId,
            TargetListingId = targetListingId,
            OverallScore = overallScore,
            Confidence = DetermineConfidence(overallScore),
            ScoreBreakdown = breakdown,
            DetectedAt = DateTimeOffset.UtcNow
        };
    }

    public void Confirm()
    {
        IsConfirmed = true;
    }

    public void LinkToReview(Guid reviewItemId)
    {
        ReviewItemId = reviewItemId;
    }

    private static MatchConfidence DetermineConfidence(double score)
    {
        return score switch
        {
            >= 0.98 => MatchConfidence.Exact,
            >= 0.90 => MatchConfidence.VeryHigh,
            >= 0.80 => MatchConfidence.High,
            >= 0.70 => MatchConfidence.Medium,
            >= 0.50 => MatchConfidence.Low,
            _ => MatchConfidence.VeryLow
        };
    }
}

/// <summary>
/// Breakdown of individual match scores.
/// </summary>
public sealed record MatchScoreBreakdown
{
    public double TitleScore { get; init; }
    public double VinScore { get; init; }
    public double ImageHashScore { get; init; }
    public double PriceScore { get; init; }
    public double MileageScore { get; init; }
    public double LocationScore { get; init; }
    public string? MatchReason { get; init; }
}
