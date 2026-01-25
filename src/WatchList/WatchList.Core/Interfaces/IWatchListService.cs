using WatchList.Core.Entities;

namespace WatchList.Core.Interfaces;

public interface IWatchListService
{
    Task<WatchedListing> AddToWatchListAsync(Guid userId, Guid listingId, decimal currentPrice, string? notes = null, CancellationToken cancellationToken = default);
    Task RemoveFromWatchListAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchedListing>> GetWatchedListingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WatchedListing?> GetWatchedListingAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchedListing>> GetChangesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateWatchedListingPriceAsync(Guid listingId, decimal newPrice, CancellationToken cancellationToken = default);
    Task UpdateNotesAsync(Guid userId, Guid listingId, string? notes, CancellationToken cancellationToken = default);
}
