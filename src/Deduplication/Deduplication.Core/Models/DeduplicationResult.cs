using Deduplication.Core.Entities;

namespace Deduplication.Core.Models;

/// <summary>
/// Result of a deduplication analysis.
/// </summary>
public sealed record DeduplicationResult
{
    public Guid AnalyzedListingId { get; init; }
    public IReadOnlyList<DuplicateMatch> Duplicates { get; init; } = [];
    public IReadOnlyList<ReviewItem> ReviewItems { get; init; } = [];
    public int TotalCandidatesChecked { get; init; }
    public TimeSpan Duration { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static DeduplicationResult Successful(
        Guid listingId,
        IReadOnlyList<DuplicateMatch> duplicates,
        IReadOnlyList<ReviewItem> reviewItems,
        int candidatesChecked,
        TimeSpan duration)
    {
        return new DeduplicationResult
        {
            AnalyzedListingId = listingId,
            Duplicates = duplicates,
            ReviewItems = reviewItems,
            TotalCandidatesChecked = candidatesChecked,
            Duration = duration,
            Success = true
        };
    }

    public static DeduplicationResult Failed(Guid listingId, string errorMessage, TimeSpan duration)
    {
        return new DeduplicationResult
        {
            AnalyzedListingId = listingId,
            Duration = duration,
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
