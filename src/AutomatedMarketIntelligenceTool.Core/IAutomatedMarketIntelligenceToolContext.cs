using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core;

public interface IAutomatedMarketIntelligenceToolContext
{
    DbSet<Listing> Listings { get; }
    DbSet<PriceHistory> PriceHistory { get; }
    DbSet<SearchSession> SearchSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
