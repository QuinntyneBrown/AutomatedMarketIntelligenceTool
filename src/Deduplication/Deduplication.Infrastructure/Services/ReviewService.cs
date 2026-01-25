using Deduplication.Core.Entities;
using Deduplication.Core.Enums;
using Deduplication.Core.Interfaces;
using Deduplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deduplication.Infrastructure.Services;

public sealed class ReviewService : IReviewService
{
    private readonly DeduplicationDbContext _context;

    public ReviewService(DeduplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewItems
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewItem>> GetPendingReviewsAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReviewItems
            .Where(r => r.Status == ReviewStatus.Pending)
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.MatchScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewItem>> GetByPriorityAsync(
        int minPriority,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReviewItems
            .Where(r => r.Status == ReviewStatus.Pending && r.Priority <= minPriority)
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.MatchScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewItem> CreateReviewItemAsync(
        Guid sourceListingId,
        Guid targetListingId,
        double matchScore,
        int priority,
        CancellationToken cancellationToken = default)
    {
        // Create a placeholder duplicate match to link to
        var breakdown = new MatchScoreBreakdown();
        var duplicateMatch = DuplicateMatch.Create(
            sourceListingId,
            targetListingId,
            matchScore,
            breakdown);

        _context.DuplicateMatches.Add(duplicateMatch);

        var reviewItem = ReviewItem.Create(
            duplicateMatch.Id,
            sourceListingId,
            targetListingId,
            matchScore);

        _context.ReviewItems.Add(reviewItem);
        await _context.SaveChangesAsync(cancellationToken);

        return reviewItem;
    }

    public async Task ResolveAsync(
        Guid reviewItemId,
        ReviewStatus status,
        string? reviewedBy,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var reviewItem = await _context.ReviewItems
            .FirstOrDefaultAsync(r => r.Id == reviewItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Review item {reviewItemId} not found");

        // Parse reviewedBy as Guid if provided
        Guid? reviewerId = null;
        if (!string.IsNullOrWhiteSpace(reviewedBy) && Guid.TryParse(reviewedBy, out var parsedId))
        {
            reviewerId = parsedId;
        }

        switch (status)
        {
            case ReviewStatus.ConfirmedDuplicate:
                if (reviewerId.HasValue)
                    reviewItem.ConfirmAsDuplicate(reviewerId.Value, notes);
                break;
            case ReviewStatus.ConfirmedNotDuplicate:
                if (reviewerId.HasValue)
                    reviewItem.ConfirmAsNotDuplicate(reviewerId.Value, notes);
                break;
            case ReviewStatus.Skipped:
                if (reviewerId.HasValue)
                    reviewItem.Skip(reviewerId.Value, notes);
                break;
        }

        // Create audit entry
        var auditEntry = AuditEntry.Create(
            $"ReviewResolved:{status}",
            reviewItemId: reviewItemId,
            details: notes,
            performedBy: reviewerId);

        _context.AuditEntries.Add(auditEntry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReviewItems
            .CountAsync(r => r.Status == ReviewStatus.Pending, cancellationToken);
    }
}
