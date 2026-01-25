using Microsoft.EntityFrameworkCore;
using Shared.Messaging;
using WatchList.Core.Entities;
using WatchList.Core.Events;
using WatchList.Core.Interfaces;
using WatchList.Infrastructure.Data;

namespace WatchList.Infrastructure.Services;

public sealed class WatchListService : IWatchListService
{
    private readonly WatchListDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public WatchListService(WatchListDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<WatchedListing> AddToWatchListAsync(Guid userId, Guid listingId, decimal currentPrice, string? notes = null, CancellationToken cancellationToken = default)
    {
        var existing = await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ListingId == listingId, cancellationToken);

        if (existing != null)
        {
            existing.UpdateNotes(notes);
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var watchedListing = WatchedListing.Create(userId, listingId, currentPrice, notes);
        _context.WatchedListings.Add(watchedListing);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ListingWatchedEvent
        {
            UserId = userId,
            ListingId = listingId,
            PriceAtWatch = currentPrice
        }, cancellationToken);

        return watchedListing;
    }

    public async Task RemoveFromWatchListAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var watchedListing = await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ListingId == listingId, cancellationToken);

        if (watchedListing == null)
            return;

        _context.WatchedListings.Remove(watchedListing);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ListingUnwatchedEvent
        {
            UserId = userId,
            ListingId = listingId
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<WatchedListing>> GetWatchedListingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.WatchedListings
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WatchedListing?> GetWatchedListingAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        return await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ListingId == listingId, cancellationToken);
    }

    public async Task<IReadOnlyList<WatchedListing>> GetChangesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.WatchedListings
            .Where(w => w.UserId == userId && w.PriceChange != 0)
            .OrderByDescending(w => Math.Abs(w.PriceChange))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateWatchedListingPriceAsync(Guid listingId, decimal newPrice, CancellationToken cancellationToken = default)
    {
        var watchedListings = await _context.WatchedListings
            .Where(w => w.ListingId == listingId)
            .ToListAsync(cancellationToken);

        foreach (var watchedListing in watchedListings)
        {
            var previousPrice = watchedListing.CurrentPrice;
            if (previousPrice != newPrice)
            {
                watchedListing.UpdatePrice(newPrice);

                await _eventPublisher.PublishAsync(new WatchedListingPriceChangedEvent
                {
                    WatchedListingId = watchedListing.Id,
                    UserId = watchedListing.UserId,
                    ListingId = listingId,
                    PreviousPrice = previousPrice,
                    NewPrice = newPrice,
                    PriceChange = newPrice - previousPrice
                }, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNotesAsync(Guid userId, Guid listingId, string? notes, CancellationToken cancellationToken = default)
    {
        var watchedListing = await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ListingId == listingId, cancellationToken)
            ?? throw new InvalidOperationException($"Watched listing for user {userId} and listing {listingId} not found");

        watchedListing.UpdateNotes(notes);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
