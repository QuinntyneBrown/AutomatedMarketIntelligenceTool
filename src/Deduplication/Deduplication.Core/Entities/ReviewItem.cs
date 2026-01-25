using Deduplication.Core.Enums;

namespace Deduplication.Core.Entities;

/// <summary>
/// Represents an item requiring manual review for duplicate confirmation.
/// </summary>
public sealed class ReviewItem
{
    public Guid Id { get; init; }
    public Guid DuplicateMatchId { get; init; }
    public Guid SourceListingId { get; init; }
    public Guid TargetListingId { get; init; }
    public double MatchScore { get; init; }
    public ReviewStatus Status { get; private set; }
    public string? ReviewNotes { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public int Priority { get; init; }

    private ReviewItem() { }

    public static ReviewItem Create(
        Guid duplicateMatchId,
        Guid sourceListingId,
        Guid targetListingId,
        double matchScore)
    {
        return new ReviewItem
        {
            Id = Guid.NewGuid(),
            DuplicateMatchId = duplicateMatchId,
            SourceListingId = sourceListingId,
            TargetListingId = targetListingId,
            MatchScore = matchScore,
            Status = ReviewStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Priority = CalculatePriority(matchScore)
        };
    }

    public void ConfirmAsDuplicate(Guid reviewerId, string? notes = null)
    {
        Status = ReviewStatus.ConfirmedDuplicate;
        ReviewedBy = reviewerId;
        ReviewNotes = notes;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmAsNotDuplicate(Guid reviewerId, string? notes = null)
    {
        Status = ReviewStatus.ConfirmedNotDuplicate;
        ReviewedBy = reviewerId;
        ReviewNotes = notes;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Skip(Guid reviewerId, string? notes = null)
    {
        Status = ReviewStatus.Skipped;
        ReviewedBy = reviewerId;
        ReviewNotes = notes;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    private static int CalculatePriority(double matchScore)
    {
        // Higher scores get higher priority (1 = highest)
        return matchScore switch
        {
            >= 0.90 => 1,
            >= 0.85 => 2,
            >= 0.80 => 3,
            >= 0.75 => 4,
            _ => 5
        };
    }
}
