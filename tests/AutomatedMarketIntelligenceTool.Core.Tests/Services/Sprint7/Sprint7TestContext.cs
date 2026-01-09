using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Sprint7;

/// <summary>
/// Test context for Sprint 7 service tests with in-memory database.
/// </summary>
public class Sprint7TestContext : DbContext, IAutomatedMarketIntelligenceToolContext
{
    public Sprint7TestContext(DbContextOptions<Sprint7TestContext> options) : base(options)
    {
    }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Models.PriceHistoryAggregate.PriceHistory>();
    public DbSet<Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Models.SearchSessionAggregate.SearchSession>();
    public DbSet<Models.SearchProfileAggregate.SearchProfile> SearchProfiles => Set<Models.SearchProfileAggregate.SearchProfile>();
    public DbSet<Models.VehicleAggregate.Vehicle> Vehicles => Set<Models.VehicleAggregate.Vehicle>();
    public DbSet<Models.ReviewQueueAggregate.ReviewItem> ReviewItems => Set<Models.ReviewQueueAggregate.ReviewItem>();
    public DbSet<Models.WatchListAggregate.WatchedListing> WatchedListings => Set<Models.WatchListAggregate.WatchedListing>();
    public DbSet<Models.AlertAggregate.Alert> Alerts => Set<Models.AlertAggregate.Alert>();
    public DbSet<Models.AlertAggregate.AlertNotification> AlertNotifications => Set<Models.AlertAggregate.AlertNotification>();
    public DbSet<Models.DealerAggregate.Dealer> Dealers => Set<Models.DealerAggregate.Dealer>();
    public DbSet<Models.ScraperHealthAggregate.ScraperHealthRecord> ScraperHealthRecords => Set<Models.ScraperHealthAggregate.ScraperHealthRecord>();
    public DbSet<Models.CacheAggregate.ResponseCacheEntry> ResponseCacheEntries => Set<Models.CacheAggregate.ResponseCacheEntry>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Models.DeduplicationAuditAggregate.AuditEntry> AuditEntries => Set<Models.DeduplicationAuditAggregate.AuditEntry>();
    public DbSet<Models.DeduplicationConfigAggregate.DeduplicationConfig> DeduplicationConfigs => Set<Models.DeduplicationConfigAggregate.DeduplicationConfig>();
    public DbSet<CustomMarket> CustomMarkets => Set<CustomMarket>();
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();
    public DbSet<ResourceThrottle> ResourceThrottles => Set<ResourceThrottle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure CustomMarket entity
        modelBuilder.Entity<CustomMarket>(entity =>
        {
            entity.HasKey(m => m.CustomMarketId);
            entity.Property(m => m.CustomMarketId)
                .HasConversion(id => id.Value, value => new CustomMarketId(value));
        });

        // Configure ScheduledReport entity
        modelBuilder.Entity<ScheduledReport>(entity =>
        {
            entity.HasKey(r => r.ScheduledReportId);
            entity.Property(r => r.ScheduledReportId)
                .HasConversion(id => id.Value, value => new ScheduledReportId(value));
            entity.Property(r => r.Format).HasConversion<string>();
            entity.Property(r => r.Schedule).HasConversion<string>();
            entity.Property(r => r.Status).HasConversion<string>();
        });

        // Configure ResourceThrottle entity
        modelBuilder.Entity<ResourceThrottle>(entity =>
        {
            entity.HasKey(t => t.ResourceThrottleId);
            entity.Property(t => t.ResourceThrottleId)
                .HasConversion(id => id.Value, value => new ResourceThrottleId(value));
            entity.Property(t => t.ResourceType).HasConversion<string>();
            entity.Property(t => t.TimeWindow).HasConversion<string>();
            entity.Property(t => t.Action).HasConversion<string>();
        });

        // Configure Report entity
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.ReportId);
            entity.Property(r => r.ReportId)
                .HasConversion(id => id.Value, value => new ReportId(value));
            entity.Property(r => r.Format).HasConversion<string>();
            entity.Property(r => r.Status).HasConversion<string>();
        });

        // Configure Listing entity
        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(l => l.ListingId);
            entity.Property(l => l.ListingId)
                .HasConversion(id => id.Value, value => new ListingId(value));
            entity.Ignore(l => l.DomainEvents);
            entity.Ignore(l => l.Location);
            entity.Ignore(l => l.Dealer);
            entity.Ignore(l => l.DealerEntity);
            entity.Ignore(l => l.DealerId);
        });

        // Configure other required entities minimally
        modelBuilder.Entity<Models.PriceHistoryAggregate.PriceHistory>(entity =>
        {
            entity.HasKey(ph => ph.PriceHistoryId);
            entity.Property(ph => ph.PriceHistoryId)
                .HasConversion(id => id.Value, value => new Models.PriceHistoryAggregate.PriceHistoryId(value));
            entity.Property(ph => ph.ListingId)
                .HasConversion(id => id.Value, value => new ListingId(value));
        });

        modelBuilder.Entity<Models.SearchSessionAggregate.SearchSession>(entity =>
        {
            entity.HasKey(ss => ss.SearchSessionId);
            entity.Property(ss => ss.SearchSessionId)
                .HasConversion(id => id.Value, value => new Models.SearchSessionAggregate.SearchSessionId(value));
        });

        modelBuilder.Entity<Models.SearchProfileAggregate.SearchProfile>(entity =>
        {
            entity.HasKey(sp => sp.SearchProfileId);
            entity.Property(sp => sp.SearchProfileId)
                .HasConversion(id => id.Value, value => Models.SearchProfileAggregate.SearchProfileId.From(value));
        });

        modelBuilder.Entity<Models.VehicleAggregate.Vehicle>(entity =>
        {
            entity.HasKey(v => v.VehicleId);
            entity.Property(v => v.VehicleId)
                .HasConversion(id => id.Value, value => new Models.VehicleAggregate.VehicleId(value));
        });

        modelBuilder.Entity<Models.ReviewQueueAggregate.ReviewItem>(entity =>
        {
            entity.HasKey(r => r.ReviewItemId);
            entity.Property(r => r.ReviewItemId)
                .HasConversion(id => id.Value, value => new Models.ReviewQueueAggregate.ReviewItemId(value));
            entity.Property(r => r.Listing1Id).HasConversion(id => id.Value, value => new ListingId(value));
            entity.Property(r => r.Listing2Id).HasConversion(id => id.Value, value => new ListingId(value));
            entity.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<Models.WatchListAggregate.WatchedListing>(entity =>
        {
            entity.HasKey(w => w.WatchedListingId);
            entity.Property(w => w.WatchedListingId).HasConversion(id => id.Value, value => new Models.WatchListAggregate.WatchedListingId(value));
            entity.Property(w => w.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
        });

        modelBuilder.Entity<Models.AlertAggregate.Alert>(entity =>
        {
            entity.HasKey(a => a.AlertId);
            entity.Property(a => a.AlertId).HasConversion(id => id.Value, value => new Models.AlertAggregate.AlertId(value));
        });

        modelBuilder.Entity<Models.AlertAggregate.AlertNotification>(entity =>
        {
            entity.HasKey(an => an.NotificationId);
            entity.Property(an => an.AlertId).HasConversion(id => id.Value, value => new Models.AlertAggregate.AlertId(value));
            entity.Property(an => an.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
        });

        modelBuilder.Entity<Models.DealerAggregate.Dealer>(entity =>
        {
            entity.HasKey(d => d.DealerId);
            entity.Property(d => d.DealerId).HasConversion(id => id.Value, value => new Models.DealerAggregate.DealerId(value));
        });

        modelBuilder.Entity<Models.ScraperHealthAggregate.ScraperHealthRecord>(entity =>
        {
            entity.HasKey(sh => sh.ScraperHealthRecordId);
            entity.Property(sh => sh.ScraperHealthRecordId).HasConversion(id => id.Value, value => new Models.ScraperHealthAggregate.ScraperHealthRecordId(value));
        });

        modelBuilder.Entity<Models.CacheAggregate.ResponseCacheEntry>(entity =>
        {
            entity.HasKey(c => c.CacheEntryId);
            entity.Property(c => c.CacheEntryId).HasConversion(id => id.Value, value => Models.CacheAggregate.ResponseCacheEntryId.FromGuid(value));
        });

        modelBuilder.Entity<Models.DeduplicationAuditAggregate.AuditEntry>(entity =>
        {
            entity.HasKey(a => a.AuditEntryId);
            entity.Property(a => a.AuditEntryId)
                .HasConversion(id => id.Value, value => new Models.DeduplicationAuditAggregate.AuditEntryId(value));
        });

        modelBuilder.Entity<Models.DeduplicationConfigAggregate.DeduplicationConfig>(entity =>
        {
            entity.HasKey(c => c.ConfigId);
            entity.Property(c => c.ConfigId)
                .HasConversion(id => id.Value, value => new Models.DeduplicationConfigAggregate.DeduplicationConfigId(value));
        });
    }
}
