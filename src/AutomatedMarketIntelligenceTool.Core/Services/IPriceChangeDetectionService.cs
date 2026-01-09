using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IPriceChangeDetectionService
{
    Task<PriceChangeDetectionResult> DetectAndRecordPriceChangesAsync(
        Guid tenantId,
        IEnumerable<Listing> listings,
        CancellationToken cancellationToken = default);
}

public class PriceChangeDetectionResult
{
    public required int TotalListings { get; init; }
    public required int PriceChangesCount { get; init; }
    public required List<PriceChangeInfo> PriceChanges { get; init; }
}

public class PriceChangeInfo
{
    public required ListingId ListingId { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int Year { get; init; }
    public required decimal OldPrice { get; init; }
    public required decimal NewPrice { get; init; }
    public required decimal PriceChange { get; init; }
    public required decimal? ChangePercentage { get; init; }
}
