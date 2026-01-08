using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DuplicateDetectionService> _logger;

    public DuplicateDetectionService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DuplicateDetectionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default)
    {
        if (scrapedListing == null)
        {
            throw new ArgumentNullException(nameof(scrapedListing));
        }

        _logger.LogDebug(
            "Checking for duplicate listing: {ExternalId} from {SourceSite} with VIN {VIN}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite,
            scrapedListing.Vin ?? "N/A");

        // 1. Check VIN match (if VIN present and valid)
        if (!string.IsNullOrWhiteSpace(scrapedListing.Vin) &&
            IsValidVin(scrapedListing.Vin))
        {
            _logger.LogDebug("Checking VIN match for {VIN}", scrapedListing.Vin);
            
            var vinMatch = await _context.Listings
                .Where(l => l.TenantId == scrapedListing.TenantId)
                .FirstOrDefaultAsync(l =>
                    l.Vin != null &&
                    l.Vin.ToUpper() == scrapedListing.Vin.ToUpper(),
                    cancellationToken);

            if (vinMatch != null)
            {
                _logger.LogInformation(
                    "Duplicate found via VIN match: {VIN}, ExistingListingId: {ListingId}",
                    scrapedListing.Vin,
                    vinMatch.ListingId.Value);
                
                return DuplicateCheckResult.VinMatch(vinMatch.ListingId.Value);
            }
        }

        // 2. Check ExternalId + SourceSite match
        _logger.LogDebug(
            "Checking ExternalId match for {ExternalId} on {SourceSite}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite);
        
        var externalIdMatch = await _context.Listings
            .Where(l => l.TenantId == scrapedListing.TenantId)
            .FirstOrDefaultAsync(l =>
                l.ExternalId == scrapedListing.ExternalId &&
                l.SourceSite == scrapedListing.SourceSite,
                cancellationToken);

        if (externalIdMatch != null)
        {
            _logger.LogInformation(
                "Duplicate found via ExternalId match: {ExternalId} on {SourceSite}, ExistingListingId: {ListingId}",
                scrapedListing.ExternalId,
                scrapedListing.SourceSite,
                externalIdMatch.ListingId.Value);
            
            return DuplicateCheckResult.ExternalIdMatch(externalIdMatch.ListingId.Value);
        }

        // 3. No match - new listing
        _logger.LogDebug(
            "No duplicate found for ExternalId: {ExternalId} from {SourceSite}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite);
        
        return DuplicateCheckResult.NewListing();
    }

    private static bool IsValidVin(string vin)
    {
        // VIN must be exactly 17 characters
        return vin.Length == 17;
    }
}
