using Deduplication.Core.Entities;
using Deduplication.Core.Enums;

namespace Deduplication.Core.Interfaces;

/// <summary>
/// Repository for duplicate match entities.
/// </summary>
public interface IDuplicateMatchRepository
{
    Task<DuplicateMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuplicateMatch>> GetByListingIdAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuplicateMatch>> GetByConfidenceAsync(
        MatchConfidence confidence,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<DuplicateMatch?> FindExistingMatchAsync(
        Guid sourceListingId,
        Guid targetListingId,
        CancellationToken cancellationToken = default);

    Task<DuplicateMatch> AddAsync(
        DuplicateMatch match,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        DuplicateMatch match,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
