using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Deduplication;

/// <summary>
/// Test context for deduplication service tests with in-memory database.
/// </summary>
public class DeduplicationTestContext : DbContext, IAutomatedMarketIntelligenceToolContext
{
    public DeduplicationTestContext(DbContextOptions<DeduplicationTestContext> options) : base(options)
    {
    }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Core.Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Core.Models.PriceHistoryAggregate.PriceHistory>();
    public DbSet<Core.Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Core.Models.SearchSessionAggregate.SearchSession>();
    public DbSet<Core.Models.SearchProfileAggregate.SearchProfile> SearchProfiles => Set<Core.Models.SearchProfileAggregate.SearchProfile>();
    public DbSet<Core.Models.VehicleAggregate.Vehicle> Vehicles => Set<Core.Models.VehicleAggregate.Vehicle>();
    public DbSet<Core.Models.ReviewQueueAggregate.ReviewItem> ReviewItems => Set<Core.Models.ReviewQueueAggregate.ReviewItem>();
    public DbSet<Core.Models.WatchListAggregate.WatchedListing> WatchedListings => Set<Core.Models.WatchListAggregate.WatchedListing>();
    public DbSet<Core.Models.AlertAggregate.Alert> Alerts => Set<Core.Models.AlertAggregate.Alert>();
    public DbSet<Core.Models.AlertAggregate.AlertNotification> AlertNotifications => Set<Core.Models.AlertAggregate.AlertNotification>();
    public DbSet<Core.Models.DealerAggregate.Dealer> Dealers => Set<Core.Models.DealerAggregate.Dealer>();
    public DbSet<Core.Models.ScraperHealthAggregate.ScraperHealthRecord> ScraperHealthRecords => Set<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>();
    public DbSet<Core.Models.CacheAggregate.ResponseCacheEntry> ResponseCacheEntries => Set<Core.Models.CacheAggregate.ResponseCacheEntry>();
    public DbSet<Core.Models.ReportAggregate.Report> Reports => Set<Core.Models.ReportAggregate.Report>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<DeduplicationConfig> DeduplicationConfigs => Set<DeduplicationConfig>();
    public DbSet<CustomMarket> CustomMarkets => Set<CustomMarket>();
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();
    public DbSet<ResourceThrottle> ResourceThrottles => Set<ResourceThrottle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // Configure AuditEntry entity
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(a => a.AuditEntryId);
            entity.Property(a => a.AuditEntryId)
                .HasConversion(id => id.Value, value => new AuditEntryId(value));
            entity.Property(a => a.Decision).HasConversion<string>();
            entity.Property(a => a.Reason).HasConversion<string>();
        });

        // Configure DeduplicationConfig entity
        modelBuilder.Entity<DeduplicationConfig>(entity =>
        {
            entity.HasKey(c => c.ConfigId);
            entity.Property(c => c.ConfigId)
                .HasConversion(id => id.Value, value => new DeduplicationConfigId(value));
        });

        // Configure CustomMarket entity
        modelBuilder.Entity<CustomMarket>(entity =>
        {
            entity.HasKey(cm => cm.CustomMarketId);
            entity.Property(cm => cm.CustomMarketId)
                .HasConversion(id => id.Value, value => new CustomMarketId(value));
        });

        // Configure ScheduledReport entity
        modelBuilder.Entity<ScheduledReport>(entity =>
        {
            entity.HasKey(sr => sr.ScheduledReportId);
            entity.Property(sr => sr.ScheduledReportId)
                .HasConversion(id => id.Value, value => new ScheduledReportId(value));
        });

        // Configure ResourceThrottle entity
        modelBuilder.Entity<ResourceThrottle>(entity =>
        {
            entity.HasKey(rt => rt.ResourceThrottleId);
            entity.Property(rt => rt.ResourceThrottleId)
                .HasConversion(id => id.Value, value => new ResourceThrottleId(value));
        });

        // Configure other entities with minimal setup
        modelBuilder.Entity<Core.Models.PriceHistoryAggregate.PriceHistory>(entity =>
        {
            entity.HasKey(ph => ph.PriceHistoryId);
            entity.Property(ph => ph.PriceHistoryId)
                .HasConversion(id => id.Value, value => new Core.Models.PriceHistoryAggregate.PriceHistoryId(value));
            entity.Property(ph => ph.ListingId)
                .HasConversion(id => id.Value, value => new ListingId(value));
        });

        modelBuilder.Entity<Core.Models.SearchSessionAggregate.SearchSession>(entity =>
        {
            entity.HasKey(ss => ss.SearchSessionId);
            entity.Property(ss => ss.SearchSessionId)
                .HasConversion(id => id.Value, value => new Core.Models.SearchSessionAggregate.SearchSessionId(value));
        });

        modelBuilder.Entity<Core.Models.SearchProfileAggregate.SearchProfile>(entity =>
        {
            entity.HasKey(sp => sp.SearchProfileId);
            entity.Property(sp => sp.SearchProfileId)
                .HasConversion(id => id.Value, value => Core.Models.SearchProfileAggregate.SearchProfileId.From(value));
        });

        modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
        {
            entity.HasKey(v => v.VehicleId);
            entity.Property(v => v.VehicleId)
                .HasConversion(id => id.Value, value => new Core.Models.VehicleAggregate.VehicleId(value));
        });

        modelBuilder.Entity<Core.Models.ReviewQueueAggregate.ReviewItem>(entity =>
        {
            entity.HasKey(r => r.ReviewItemId);
            entity.Property(r => r.ReviewItemId)
                .HasConversion(id => id.Value, value => new Core.Models.ReviewQueueAggregate.ReviewItemId(value));
            entity.Property(r => r.Listing1Id).HasConversion(id => id.Value, value => new ListingId(value));
            entity.Property(r => r.Listing2Id).HasConversion(id => id.Value, value => new ListingId(value));
            entity.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<Core.Models.WatchListAggregate.WatchedListing>(entity =>
        {
            entity.HasKey(w => w.WatchedListingId);
            entity.Property(w => w.WatchedListingId).HasConversion(id => id.Value, value => new Core.Models.WatchListAggregate.WatchedListingId(value));
            entity.Property(w => w.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
        });

        modelBuilder.Entity<Core.Models.AlertAggregate.Alert>(entity =>
        {
            entity.HasKey(a => a.AlertId);
            entity.Property(a => a.AlertId).HasConversion(id => id.Value, value => new Core.Models.AlertAggregate.AlertId(value));
        });

        modelBuilder.Entity<Core.Models.AlertAggregate.AlertNotification>(entity =>
        {
            entity.HasKey(an => an.NotificationId);
            entity.Property(an => an.AlertId).HasConversion(id => id.Value, value => new Core.Models.AlertAggregate.AlertId(value));
            entity.Property(an => an.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
        });

        modelBuilder.Entity<Core.Models.DealerAggregate.Dealer>(entity =>
        {
            entity.HasKey(d => d.DealerId);
            entity.Property(d => d.DealerId).HasConversion(id => id.Value, value => new Core.Models.DealerAggregate.DealerId(value));
        });

        modelBuilder.Entity<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>(entity =>
        {
            entity.HasKey(sh => sh.ScraperHealthRecordId);
            entity.Property(sh => sh.ScraperHealthRecordId).HasConversion(id => id.Value, value => new Core.Models.ScraperHealthAggregate.ScraperHealthRecordId(value));
        });

        modelBuilder.Entity<Core.Models.CacheAggregate.ResponseCacheEntry>(entity =>
        {
            entity.HasKey(c => c.CacheEntryId);
            entity.Property(c => c.CacheEntryId).HasConversion(id => id.Value, value => Core.Models.CacheAggregate.ResponseCacheEntryId.FromGuid(value));
        });

        modelBuilder.Entity<Core.Models.ReportAggregate.Report>(entity =>
        {
            entity.HasKey(r => r.ReportId);
            entity.Property(r => r.ReportId).HasConversion(id => id.Value, value => new Core.Models.ReportAggregate.ReportId(value));
        });
    }
}
