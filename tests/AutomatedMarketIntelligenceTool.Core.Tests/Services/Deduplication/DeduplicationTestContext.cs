using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;
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
    public DbSet<Models.ReportAggregate.Report> Reports => Set<Models.ReportAggregate.Report>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<DeduplicationConfig> DeduplicationConfigs => Set<DeduplicationConfig>();

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

        // Configure other entities with minimal setup
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

        modelBuilder.Entity<Models.ReportAggregate.Report>(entity =>
        {
            entity.HasKey(r => r.ReportId);
            entity.Property(r => r.ReportId).HasConversion(id => id.Value, value => new Models.ReportAggregate.ReportId(value));
        });
    }
}
