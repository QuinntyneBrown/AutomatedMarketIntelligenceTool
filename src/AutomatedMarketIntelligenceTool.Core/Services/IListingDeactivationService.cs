using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IListingDeactivationService
{
    Task<ListingDeactivationResult> DeactivateStaleListingsAsync(
        Guid tenantId,
        int staleDays = 7,
        CancellationToken cancellationToken = default);
}

public class ListingDeactivationResult
{
    public required int TotalActiveListings { get; init; }
    public required int DeactivatedCount { get; init; }
    public required List<ListingId> DeactivatedListingIds { get; init; }
}
