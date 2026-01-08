using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core;

public interface IAutomatedMarketIntelligenceToolContext
{
    DbSet<Listing> Listings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
