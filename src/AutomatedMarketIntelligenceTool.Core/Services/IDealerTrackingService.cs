using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IDealerTrackingService
{
    Task<Dealer> GetOrCreateDealerAsync(
        Guid tenantId,
        string dealerName,
        string? city = null,
        string? state = null,
        string? phone = null,
        CancellationToken cancellationToken = default);

    Task<Dealer?> FindDealerByNameAsync(
        Guid tenantId,
        string dealerName,
        CancellationToken cancellationToken = default);

    Task<List<Dealer>> GetAllDealersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Dealer> GetDealerAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);

    Task<List<Listing>> GetDealerListingsAsync(
        Guid tenantId,
        DealerId dealerId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task LinkListingToDealerAsync(
        Listing listing,
        CancellationToken cancellationToken = default);

    Task UpdateDealerStatsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default);
}
