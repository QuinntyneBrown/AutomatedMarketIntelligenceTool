using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class WatchListService : IWatchListService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<WatchListService> _logger;

    public WatchListService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<WatchListService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WatchedListing> AddToWatchListAsync(
        Guid tenantId,
        ListingId listingId,
        string? notes = null,
        bool notifyOnPriceChange = true,
        bool notifyOnRemoval = true,
        CancellationToken cancellationToken = default)
    {
        // Check if already watched
        var existing = await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.ListingId == listingId, cancellationToken);

        if (existing != null)
        {
            _logger.LogInformation("Listing {ListingId} is already in watch list for tenant {TenantId}", listingId.Value, tenantId);
            return existing;
        }

        // Verify listing exists
        var listing = await _context.Listings
            .FirstOrDefaultAsync(l => l.ListingId == listingId, cancellationToken);

        if (listing == null)
        {
            throw new ArgumentException($"Listing with ID {listingId.Value} not found", nameof(listingId));
        }

        var watchedListing = WatchedListing.Create(tenantId, listingId, notes, notifyOnPriceChange, notifyOnRemoval);
        
        _context.WatchedListings.Add(watchedListing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added listing {ListingId} to watch list for tenant {TenantId}", listingId.Value, tenantId);
        
        return watchedListing;
    }

    public async Task RemoveFromWatchListAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default)
    {
        var watchedListing = await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.ListingId == listingId, cancellationToken);

        if (watchedListing == null)
        {
            _logger.LogWarning("Listing {ListingId} not found in watch list for tenant {TenantId}", listingId.Value, tenantId);
            return;
        }

        _context.WatchedListings.Remove(watchedListing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed listing {ListingId} from watch list for tenant {TenantId}", listingId.Value, tenantId);
    }

    public async Task<WatchedListing?> GetWatchedListingAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WatchedListings
            .Include(w => w.Listing)
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.ListingId == listingId, cancellationToken);
    }

    public async Task<List<WatchedListing>> GetAllWatchedListingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WatchedListings
            .Include(w => w.Listing)
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateNotesAsync(
        Guid tenantId,
        ListingId listingId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var watchedListing = await _context.WatchedListings
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.ListingId == listingId, cancellationToken);

        if (watchedListing == null)
        {
            throw new ArgumentException($"Watched listing with ID {listingId.Value} not found", nameof(listingId));
        }

        watchedListing.UpdateNotes(notes);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated notes for watched listing {ListingId}", listingId.Value);
    }

    public async Task<bool> IsWatchedAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WatchedListings
            .AnyAsync(w => w.TenantId == tenantId && w.ListingId == listingId, cancellationToken);
    }
}
