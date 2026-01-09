using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class DealerTrackingService : IDealerTrackingService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DealerTrackingService> _logger;

    public DealerTrackingService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DealerTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dealer> GetOrCreateDealerAsync(
        Guid tenantId,
        string dealerName,
        string? city = null,
        string? state = null,
        string? phone = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dealerName))
        {
            throw new ArgumentException("Dealer name cannot be empty", nameof(dealerName));
        }

        var normalizedName = Dealer.NormalizeDealerName(dealerName);

        var dealer = await _context.Dealers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.NormalizedName == normalizedName, cancellationToken);

        if (dealer == null)
        {
            dealer = Dealer.Create(tenantId, dealerName, city, state, phone);
            _context.Dealers.Add(dealer);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new dealer: {DealerName} (normalized: {NormalizedName})", 
                dealerName, normalizedName);
        }
        else
        {
            // Update contact info if provided and different
            if (!string.IsNullOrWhiteSpace(city) || !string.IsNullOrWhiteSpace(state) || !string.IsNullOrWhiteSpace(phone))
            {
                dealer.UpdateContactInfo(city, state, phone);
                dealer.UpdateLastSeen();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return dealer;
    }

    public async Task<Dealer?> FindDealerByNameAsync(
        Guid tenantId,
        string dealerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dealerName))
        {
            return null;
        }

        var normalizedName = Dealer.NormalizeDealerName(dealerName);

        return await _context.Dealers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<List<Dealer>> GetAllDealersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.ListingCount)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dealer> GetDealerAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var dealer = await _context.Dealers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DealerId == dealerId, cancellationToken);

        if (dealer == null)
        {
            throw new ArgumentException($"Dealer with ID {dealerId.Value} not found", nameof(dealerId));
        }

        return dealer;
    }

    public async Task<List<Listing>> GetDealerListingsAsync(
        Guid tenantId,
        DealerId dealerId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Listings
            .Where(l => l.TenantId == tenantId && l.DealerId == dealerId);

        if (activeOnly)
        {
            query = query.Where(l => l.IsActive);
        }

        return await query
            .OrderByDescending(l => l.FirstSeenDate)
            .ToListAsync(cancellationToken);
    }

    public async Task LinkListingToDealerAsync(
        Listing listing,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(listing.SellerName))
        {
            _logger.LogDebug("Listing {ListingId} has no seller name, skipping dealer linking", 
                listing.ListingId.Value);
            return;
        }

        var dealer = await GetOrCreateDealerAsync(
            listing.TenantId,
            listing.SellerName,
            listing.City,
            listing.Province,
            listing.SellerPhone,
            cancellationToken);

        if (listing.DealerId == null || listing.DealerId != dealer.DealerId)
        {
            listing.SetDealer(dealer.DealerId);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Linked listing {ListingId} to dealer {DealerId}", 
                listing.ListingId.Value, dealer.DealerId.Value);
        }

        // Update dealer stats
        await UpdateDealerStatsAsync(listing.TenantId, dealer.DealerId, cancellationToken);
    }

    public async Task UpdateDealerStatsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var dealer = await GetDealerAsync(tenantId, dealerId, cancellationToken);

        var activeListingCount = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.DealerId == dealerId && l.IsActive)
            .CountAsync(cancellationToken);

        // Only increment if there are active listings
        if (activeListingCount > 0)
        {
            dealer.IncrementListingCount();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Updated dealer {DealerId} stats: {Count} active listings", 
                dealerId.Value, activeListingCount);
        }
    }
}
