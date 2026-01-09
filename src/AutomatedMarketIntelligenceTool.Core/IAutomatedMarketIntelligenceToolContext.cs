using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core;

public interface IAutomatedMarketIntelligenceToolContext
{
    DbSet<Listing> Listings { get; }
    DbSet<PriceHistory> PriceHistory { get; }
    DbSet<SearchSession> SearchSessions { get; }
    DbSet<SearchProfile> SearchProfiles { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<ReviewItem> ReviewItems { get; }
    DbSet<WatchedListing> WatchedListings { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<AlertNotification> AlertNotifications { get; }
    DbSet<Dealer> Dealers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
