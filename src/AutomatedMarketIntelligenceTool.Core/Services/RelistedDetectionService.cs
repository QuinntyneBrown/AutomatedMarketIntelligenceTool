using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Implementation of the relisted detection service.
/// </summary>
public class RelistedDetectionService : IRelistedDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<RelistedDetectionService> _logger;

    private const int MinDaysOffMarket = 1;  // Minimum days to consider as relisting
    private const double VinMatchConfidence = 100.0;
    private const double ExternalIdMatchConfidence = 95.0;

    public RelistedDetectionService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<RelistedDetectionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RelistingCheckResult> CheckForRelistingAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default)
    {
        if (scrapedListing == null)
        {
            throw new ArgumentNullException(nameof(scrapedListing));
        }

        _logger.LogDebug(
            "Checking for relisting: {ExternalId} from {SourceSite}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite);

        Listing? previousListing = null;
        double matchConfidence = 0;

        // 1. Check VIN match for deactivated listings
        if (!string.IsNullOrWhiteSpace(scrapedListing.Vin) && scrapedListing.Vin.Length == 17)
        {
            previousListing = await _context.Listings
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == scrapedListing.TenantId)
                .Where(l => !l.IsActive)
                .Where(l => l.Vin != null && l.Vin.ToUpper() == scrapedListing.Vin.ToUpper())
                .OrderByDescending(l => l.DeactivatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (previousListing != null)
            {
                matchConfidence = VinMatchConfidence;
            }
        }

        // 2. Check ExternalId + SourceSite for deactivated listings
        if (previousListing == null)
        {
            previousListing = await _context.Listings
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == scrapedListing.TenantId)
                .Where(l => !l.IsActive)
                .Where(l => l.ExternalId == scrapedListing.ExternalId)
                .Where(l => l.SourceSite == scrapedListing.SourceSite)
                .OrderByDescending(l => l.DeactivatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (previousListing != null)
            {
                matchConfidence = ExternalIdMatchConfidence;
            }
        }

        if (previousListing == null)
        {
            _logger.LogDebug("No previous deactivated listing found");
            return RelistingCheckResult.NotRelisted();
        }

        // Check if it's been off market long enough
        var timeOffMarket = DateTime.UtcNow - (previousListing.DeactivatedAt ?? previousListing.LastSeenDate);
        if (timeOffMarket.TotalDays < MinDaysOffMarket)
        {
            _logger.LogDebug(
                "Listing was only off market for {Days:F1} days, not considered relisting",
                timeOffMarket.TotalDays);
            return RelistingCheckResult.NotRelisted();
        }

        // Calculate price difference
        var currentPrice = scrapedListing.Price ?? 0;
        var priceDelta = currentPrice - previousListing.Price;

        _logger.LogInformation(
            "Detected relisting: {ExternalId} from {SourceSite}, was off market for {Days:F0} days, price delta: ${PriceDelta:N0}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite,
            timeOffMarket.TotalDays,
            priceDelta);

        return RelistingCheckResult.Relisted(
            previousListing,
            timeOffMarket,
            priceDelta,
            matchConfidence);
    }

    public async Task<RelistingStats> GetStatsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var relistedListings = await _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId)
            .Where(l => l.RelistedCount > 0)
            .ToListAsync(cancellationToken);

        if (relistedListings.Count == 0)
        {
            return new RelistingStats();
        }

        return new RelistingStats
        {
            TotalRelistedCount = relistedListings.Count,
            ActiveRelistedCount = relistedListings.Count(l => l.IsActive),
            AverageTimeOffMarketDays = 0, // Would require additional tracking
            AveragePriceDelta = 0, // Would require price history analysis
            AveragePriceChangePercentage = 0,
            FrequentRelisterCount = relistedListings.Count(l => l.RelistedCount >= 3)
        };
    }

    public async Task<IReadOnlyList<RelistedListingInfo>> GetRelistedListingsAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var listings = await _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId)
            .Where(l => l.RelistedCount > 0)
            .OrderByDescending(l => l.RelistedCount)
            .ThenByDescending(l => l.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return listings.Select(l => new RelistedListingInfo
        {
            ListingId = l.ListingId,
            Make = l.Make,
            Model = l.Model,
            Year = l.Year,
            CurrentPrice = l.Price,
            RelistedCount = l.RelistedCount,
            ReactivatedAt = l.UpdatedAt ?? l.LastSeenDate,
            SourceSite = l.SourceSite
        }).ToList().AsReadOnly();
    }
}
