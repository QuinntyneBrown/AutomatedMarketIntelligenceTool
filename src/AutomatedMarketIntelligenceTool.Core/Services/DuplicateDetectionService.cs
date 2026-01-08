using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;

    public DuplicateDetectionService(IAutomatedMarketIntelligenceToolContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default)
    {
        if (scrapedListing == null)
        {
            throw new ArgumentNullException(nameof(scrapedListing));
        }

        // 1. Check VIN match (if VIN present and valid)
        if (!string.IsNullOrWhiteSpace(scrapedListing.Vin) &&
            IsValidVin(scrapedListing.Vin))
        {
            var vinMatch = await _context.Listings
                .Where(l => l.TenantId == scrapedListing.TenantId)
                .FirstOrDefaultAsync(l =>
                    l.Vin != null &&
                    l.Vin.ToUpper() == scrapedListing.Vin.ToUpper(),
                    cancellationToken);

            if (vinMatch != null)
            {
                return DuplicateCheckResult.VinMatch(vinMatch.ListingId.Value);
            }
        }

        // 2. Check ExternalId + SourceSite match
        var externalIdMatch = await _context.Listings
            .Where(l => l.TenantId == scrapedListing.TenantId)
            .FirstOrDefaultAsync(l =>
                l.ExternalId == scrapedListing.ExternalId &&
                l.SourceSite == scrapedListing.SourceSite,
                cancellationToken);

        if (externalIdMatch != null)
        {
            return DuplicateCheckResult.ExternalIdMatch(externalIdMatch.ListingId.Value);
        }

        // 3. No match - new listing
        return DuplicateCheckResult.NewListing();
    }

    private static bool IsValidVin(string vin)
    {
        // VIN must be exactly 17 characters
        return vin.Length == 17;
    }
}
