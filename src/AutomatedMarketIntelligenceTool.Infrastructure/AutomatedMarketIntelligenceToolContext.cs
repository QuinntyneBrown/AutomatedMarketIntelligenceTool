using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Infrastructure;

public class AutomatedMarketIntelligenceToolContext : DbContext, IAutomatedMarketIntelligenceToolContext
{
    private readonly Guid _tenantId;

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
    public DbSet<SearchSession> SearchSessions => Set<SearchSession>();
    public DbSet<SearchProfile> SearchProfiles => Set<SearchProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
    public DbSet<WatchedListing> WatchedListings => Set<WatchedListing>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertNotification> AlertNotifications => Set<AlertNotification>();
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<ScraperHealthRecord> ScraperHealthRecords => Set<ScraperHealthRecord>();
    public DbSet<ResponseCacheEntry> ResponseCacheEntries => Set<ResponseCacheEntry>();
    public DbSet<Report> Reports => Set<Report>();

    // Phase 5 Analytics
    public DbSet<DealerMetrics> DealerMetrics => Set<DealerMetrics>();
    public DbSet<RelistingPattern> RelistingPatterns => Set<RelistingPattern>();
    public DbSet<DealerDeduplicationRule> DealerDeduplicationRules => Set<DealerDeduplicationRule>();
    public DbSet<InventorySnapshot> InventorySnapshots => Set<InventorySnapshot>();

    public AutomatedMarketIntelligenceToolContext(DbContextOptions<AutomatedMarketIntelligenceToolContext> options)
        : base(options)
    {
        // TODO: Inject ITenantContext to get TenantId
        // For now, using a default value
        _tenantId = Guid.Empty;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutomatedMarketIntelligenceToolContext).Assembly);

        // Apply global query filter for multi-tenancy
        modelBuilder.Entity<Listing>().HasQueryFilter(l => l.TenantId == _tenantId);
        modelBuilder.Entity<PriceHistory>().HasQueryFilter(ph => ph.TenantId == _tenantId);
        modelBuilder.Entity<SearchSession>().HasQueryFilter(ss => ss.TenantId == _tenantId);
        modelBuilder.Entity<SearchProfile>().HasQueryFilter(sp => sp.TenantId == _tenantId);
        modelBuilder.Entity<Vehicle>().HasQueryFilter(v => v.TenantId == _tenantId);
        modelBuilder.Entity<ReviewItem>().HasQueryFilter(r => r.TenantId == _tenantId);
        modelBuilder.Entity<WatchedListing>().HasQueryFilter(w => w.TenantId == _tenantId);
        modelBuilder.Entity<Alert>().HasQueryFilter(a => a.TenantId == _tenantId);
        modelBuilder.Entity<AlertNotification>().HasQueryFilter(an => an.TenantId == _tenantId);
        modelBuilder.Entity<Dealer>().HasQueryFilter(d => d.TenantId == _tenantId);

        // Phase 5 Analytics query filters
        modelBuilder.Entity<DealerMetrics>().HasQueryFilter(m => m.TenantId == _tenantId);
        modelBuilder.Entity<RelistingPattern>().HasQueryFilter(p => p.TenantId == _tenantId);
        modelBuilder.Entity<DealerDeduplicationRule>().HasQueryFilter(r => r.TenantId == _tenantId);
        modelBuilder.Entity<InventorySnapshot>().HasQueryFilter(s => s.TenantId == _tenantId);
    }
}
