using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IWatchListService
{
    Task<WatchedListing> AddToWatchListAsync(
        Guid tenantId,
        ListingId listingId,
        string? notes = null,
        bool notifyOnPriceChange = true,
        bool notifyOnRemoval = true,
        CancellationToken cancellationToken = default);

    Task RemoveFromWatchListAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default);

    Task<WatchedListing?> GetWatchedListingAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default);

    Task<List<WatchedListing>> GetAllWatchedListingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task UpdateNotesAsync(
        Guid tenantId,
        ListingId listingId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<bool> IsWatchedAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default);
}
