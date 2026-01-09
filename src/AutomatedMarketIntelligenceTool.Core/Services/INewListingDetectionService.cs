using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface INewListingDetectionService
{
    Task<NewListingDetectionResult> DetectNewListingsAsync(
        IEnumerable<Listing> listings,
        CancellationToken cancellationToken = default);
}

public class NewListingDetectionResult
{
    public required int TotalListings { get; init; }
    public required int NewListingsCount { get; init; }
    public required List<ListingId> NewListingIds { get; init; }
}
