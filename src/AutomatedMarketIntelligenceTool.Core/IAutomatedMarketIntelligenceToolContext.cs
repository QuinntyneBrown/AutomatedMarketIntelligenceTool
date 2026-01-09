using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
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
    DbSet<ScraperHealthRecord> ScraperHealthRecords { get; }
    DbSet<ResponseCacheEntry> ResponseCacheEntries { get; }
    DbSet<Report> Reports { get; }
    DbSet<AuditEntry> AuditEntries { get; }
    DbSet<DeduplicationConfig> DeduplicationConfigs { get; }
    DbSet<CustomMarket> CustomMarkets { get; }
    DbSet<ScheduledReport> ScheduledReports { get; }
    DbSet<ResourceThrottle> ResourceThrottles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
