using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class ListingDeactivationService : IListingDeactivationService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<ListingDeactivationService> _logger;

    public ListingDeactivationService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<ListingDeactivationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ListingDeactivationResult> DeactivateStaleListingsAsync(
        Guid tenantId,
        int staleDays = 7,
        CancellationToken cancellationToken = default)
    {
        if (staleDays <= 0)
        {
            throw new ArgumentException("staleDays must be greater than 0", nameof(staleDays));
        }

        _logger.LogInformation(
            "Starting stale listing deactivation for TenantId: {TenantId}, StaleDays: {StaleDays}",
            tenantId,
            staleDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-staleDays);

        var activeListings = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.IsActive)
            .ToListAsync(cancellationToken);

        var staleListings = activeListings
            .Where(l => l.LastSeenDate < cutoffDate)
            .ToList();

        foreach (var listing in staleListings)
        {
            listing.Deactivate();
            _logger.LogInformation(
                "Deactivated stale listing {ListingId} ({Year} {Make} {Model}). Last seen: {LastSeenDate}",
                listing.ListingId,
                listing.Year,
                listing.Make,
                listing.Model,
                listing.LastSeenDate);
        }

        if (staleListings.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Deactivated {DeactivatedCount} stale listings out of {TotalActiveListings} active listings",
                staleListings.Count,
                activeListings.Count);
        }
        else
        {
            _logger.LogInformation(
                "No stale listings found. All {TotalActiveListings} active listings are current",
                activeListings.Count);
        }

        return new ListingDeactivationResult
        {
            TotalActiveListings = activeListings.Count,
            DeactivatedCount = staleListings.Count,
            DeactivatedListingIds = staleListings.Select(l => l.ListingId).ToList()
        };
    }
}
