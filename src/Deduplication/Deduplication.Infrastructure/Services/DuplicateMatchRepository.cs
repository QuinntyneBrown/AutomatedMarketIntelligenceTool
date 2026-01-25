using Deduplication.Core.Entities;
using Deduplication.Core.Enums;
using Deduplication.Core.Interfaces;
using Deduplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deduplication.Infrastructure.Services;

public sealed class DuplicateMatchRepository : IDuplicateMatchRepository
{
    private readonly DeduplicationDbContext _context;

    public DuplicateMatchRepository(DeduplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DuplicateMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DuplicateMatches
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateMatch>> GetByListingIdAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DuplicateMatches
            .Where(m => m.SourceListingId == listingId || m.TargetListingId == listingId)
            .OrderByDescending(m => m.OverallScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateMatch>> GetByConfidenceAsync(
        MatchConfidence confidence,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.DuplicateMatches
            .Where(m => m.Confidence == confidence)
            .OrderByDescending(m => m.DetectedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<DuplicateMatch?> FindExistingMatchAsync(
        Guid sourceListingId,
        Guid targetListingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DuplicateMatches
            .FirstOrDefaultAsync(m =>
                (m.SourceListingId == sourceListingId && m.TargetListingId == targetListingId) ||
                (m.SourceListingId == targetListingId && m.TargetListingId == sourceListingId),
                cancellationToken);
    }

    public async Task<DuplicateMatch> AddAsync(
        DuplicateMatch match,
        CancellationToken cancellationToken = default)
    {
        _context.DuplicateMatches.Add(match);
        await _context.SaveChangesAsync(cancellationToken);
        return match;
    }

    public async Task UpdateAsync(
        DuplicateMatch match,
        CancellationToken cancellationToken = default)
    {
        _context.DuplicateMatches.Update(match);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await _context.DuplicateMatches
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (match != null)
        {
            _context.DuplicateMatches.Remove(match);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
