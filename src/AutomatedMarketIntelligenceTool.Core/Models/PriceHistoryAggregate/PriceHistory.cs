using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;

public class PriceHistory
{
    public PriceHistoryId PriceHistoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public ListingId ListingId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime ObservedAt { get; private set; }
    public decimal? PriceChange { get; private set; }
    public decimal? ChangePercentage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PriceHistory()
    {
        PriceHistoryId = PriceHistoryId.Create();
        ListingId = ListingId.Create();
    }

    public static PriceHistory Create(
        Guid tenantId,
        ListingId listingId,
        decimal price,
        decimal? previousPrice = null)
    {
        var history = new PriceHistory
        {
            PriceHistoryId = PriceHistoryId.Create(),
            TenantId = tenantId,
            ListingId = listingId,
            Price = price,
            ObservedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        if (previousPrice.HasValue && previousPrice.Value != price)
        {
            history.PriceChange = price - previousPrice.Value;
            history.ChangePercentage = previousPrice.Value != 0
                ? Math.Round((history.PriceChange.Value / previousPrice.Value) * 100, 2)
                : null;
        }

        return history;
    }
}
