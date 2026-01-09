using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class NewListingDetectionService : INewListingDetectionService
{
    private readonly ILogger<NewListingDetectionService> _logger;

    public NewListingDetectionService(ILogger<NewListingDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<NewListingDetectionResult> DetectNewListingsAsync(
        IEnumerable<Listing> listings,
        CancellationToken cancellationToken = default)
    {
        if (listings == null)
        {
            throw new ArgumentNullException(nameof(listings));
        }

        var listingsList = listings.ToList();
        var newListings = listingsList.Where(l => l.IsNewListing).ToList();

        _logger.LogInformation(
            "New listing detection completed. Found {NewListingsCount} new listings out of {TotalListings} total",
            newListings.Count,
            listingsList.Count);

        if (newListings.Count > 0)
        {
            _logger.LogDebug(
                "New listings detected: {NewListingIds}",
                string.Join(", ", newListings.Select(l => $"{l.Year} {l.Make} {l.Model}")));
        }

        var result = new NewListingDetectionResult
        {
            TotalListings = listingsList.Count,
            NewListingsCount = newListings.Count,
            NewListingIds = newListings.Select(l => l.ListingId).ToList()
        };

        return Task.FromResult(result);
    }
}
