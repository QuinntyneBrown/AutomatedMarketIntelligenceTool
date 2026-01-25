using Deduplication.Core.Models;

namespace Deduplication.Core.Interfaces;

/// <summary>
/// Service for detecting duplicate listings.
/// </summary>
public interface IDuplicateDetectionService
{
    Task<DeduplicationResult> DetectDuplicatesAsync(
        ListingData listing,
        CancellationToken cancellationToken = default);

    Task<DeduplicationResult> DetectDuplicatesAsync(
        ListingData listing,
        IEnumerable<ListingData> candidates,
        CancellationToken cancellationToken = default);

    Task ProcessBatchAsync(
        IEnumerable<Guid> listingIds,
        CancellationToken cancellationToken = default);
}
