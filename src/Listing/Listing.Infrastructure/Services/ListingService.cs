using Listing.Core.Entities;
using Listing.Core.Events;
using Listing.Core.Services;
using Listing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Messaging;

namespace Listing.Infrastructure.Services;

/// <summary>
/// Service implementation for listing operations.
/// </summary>
public sealed class ListingService : IListingService
{
    private readonly ListingDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ListingService> _logger;

    public ListingService(
        ListingDbContext dbContext,
        IEventPublisher eventPublisher,
        ILogger<ListingService> logger)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Core.Entities.Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Listings.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Core.Entities.Listing?> GetBySourceIdAsync(string source, string sourceListingId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Listings
            .FirstOrDefaultAsync(x => x.Source == source && x.SourceListingId == sourceListingId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Core.Entities.Listing> CreateAsync(Core.Entities.Listing listing, CancellationToken cancellationToken = default)
    {
        _dbContext.Listings.Add(listing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ListingCreatedEvent
        {
            ListingId = listing.Id,
            SourceListingId = listing.SourceListingId,
            Source = listing.Source,
            Title = listing.Title,
            Price = listing.Price,
            Make = listing.Make,
            Model = listing.Model,
            Year = listing.Year,
            VIN = listing.VIN,
            PrimaryImageUrl = listing.ImageUrls.FirstOrDefault()
        }, cancellationToken);

        _logger.LogInformation("Created listing {ListingId} from {Source}", listing.Id, listing.Source);

        return listing;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Core.Entities.Listing listing, CancellationToken cancellationToken = default)
    {
        _dbContext.Listings.Update(listing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ListingUpdatedEvent
        {
            ListingId = listing.Id,
            Source = listing.Source
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await GetByIdAsync(id, cancellationToken);
        if (listing == null) return;

        listing.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ListingDeletedEvent
        {
            ListingId = listing.Id,
            Source = listing.Source,
            SourceListingId = listing.SourceListingId
        }, cancellationToken);

        _logger.LogInformation("Deactivated listing {ListingId}", id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Core.Entities.Listing>> SearchAsync(
        string? make = null,
        string? model = null,
        int? yearFrom = null,
        int? yearTo = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? maxMileage = null,
        string? province = null,
        bool? isActive = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Listings.AsQueryable();

        if (!string.IsNullOrEmpty(make))
            query = query.Where(x => x.Make == make);

        if (!string.IsNullOrEmpty(model))
            query = query.Where(x => x.Model == model);

        if (yearFrom.HasValue)
            query = query.Where(x => x.Year >= yearFrom);

        if (yearTo.HasValue)
            query = query.Where(x => x.Year <= yearTo);

        if (minPrice.HasValue)
            query = query.Where(x => x.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(x => x.Price <= maxPrice);

        if (maxMileage.HasValue)
            query = query.Where(x => x.Mileage <= maxMileage);

        if (!string.IsNullOrEmpty(province))
            query = query.Where(x => x.Province == province);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive);

        return await query
            .OrderByDescending(x => x.LastSeenAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceHistory>> GetPriceHistoryAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => x.ListingId == listingId)
            .OrderByDescending(x => x.RecordedAt)
            .ToListAsync(cancellationToken);
    }
}
