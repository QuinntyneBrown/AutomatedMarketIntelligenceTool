using Deduplication.Core.Entities;
using Deduplication.Core.Enums;

namespace Deduplication.Core.Interfaces;

/// <summary>
/// Service for managing manual review items.
/// </summary>
public interface IReviewService
{
    Task<ReviewItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewItem>> GetPendingReviewsAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewItem>> GetByPriorityAsync(
        int minPriority,
        CancellationToken cancellationToken = default);

    Task<ReviewItem> CreateReviewItemAsync(
        Guid sourceListingId,
        Guid targetListingId,
        double matchScore,
        int priority,
        CancellationToken cancellationToken = default);

    Task ResolveAsync(
        Guid reviewItemId,
        ReviewStatus status,
        string? reviewedBy,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}
