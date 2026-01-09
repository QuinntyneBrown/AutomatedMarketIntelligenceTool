using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core;

public interface IAutomatedMarketIntelligenceToolContext
{
    DbSet<Listing> Listings { get; }
    DbSet<PriceHistory> PriceHistory { get; }
    DbSet<SearchSession> SearchSessions { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<ReviewItem> ReviewItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
