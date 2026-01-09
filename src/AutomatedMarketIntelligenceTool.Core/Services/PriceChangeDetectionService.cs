using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class PriceChangeDetectionService : IPriceChangeDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<PriceChangeDetectionService> _logger;

    public PriceChangeDetectionService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<PriceChangeDetectionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PriceChangeDetectionResult> DetectAndRecordPriceChangesAsync(
        Guid tenantId,
        IEnumerable<Listing> listings,
        CancellationToken cancellationToken = default)
    {
        if (listings == null)
        {
            throw new ArgumentNullException(nameof(listings));
        }

        var listingsList = listings.ToList();
        var priceChanges = new List<PriceChangeInfo>();

        foreach (var listing in listingsList)
        {
            var existingListing = await _context.Listings
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ListingId == listing.ListingId, cancellationToken);

            if (existingListing != null && existingListing.Price != listing.Price)
            {
                var priceHistory = PriceHistory.Create(
                    tenantId,
                    listing.ListingId,
                    listing.Price,
                    existingListing.Price);

                _context.PriceHistory.Add(priceHistory);

                _logger.LogInformation(
                    "Price change detected for {ListingId} ({Year} {Make} {Model}): ${OldPrice} -> ${NewPrice} ({Change:+0.00;-0.00}%)",
                    listing.ListingId,
                    listing.Year,
                    listing.Make,
                    listing.Model,
                    existingListing.Price,
                    listing.Price,
                    priceHistory.ChangePercentage);

                priceChanges.Add(new PriceChangeInfo
                {
                    ListingId = listing.ListingId,
                    Make = listing.Make,
                    Model = listing.Model,
                    Year = listing.Year,
                    OldPrice = existingListing.Price,
                    NewPrice = listing.Price,
                    PriceChange = priceHistory.PriceChange ?? 0,
                    ChangePercentage = priceHistory.ChangePercentage
                });
            }
        }

        if (priceChanges.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Recorded {PriceChangesCount} price changes in PriceHistory",
                priceChanges.Count);
        }

        return new PriceChangeDetectionResult
        {
            TotalListings = listingsList.Count,
            PriceChangesCount = priceChanges.Count,
            PriceChanges = priceChanges
        };
    }
}
