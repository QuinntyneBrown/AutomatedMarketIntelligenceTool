using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class RelistedDetectionServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly RelistedDetectionService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public RelistedDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContextWithRelisted>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContextWithRelisted(options);
        _service = new RelistedDetectionService(_context, NullLogger<RelistedDetectionService>.Instance);
    }

    [Fact]
    public async Task CheckForRelistingAsync_WithNoPreviousListing_ShouldReturnNotRelisted()
    {
        // Arrange
        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-NEW",
            SourceSite = "TestSite",
            Vin = "1HGBH41JXMN109186"
        };

        // Act
        var result = await _service.CheckForRelistingAsync(scrapedInfo);

        // Assert
        Assert.False(result.IsRelisted);
        Assert.Null(result.PreviousListing);
        Assert.Null(result.PreviousListingId);
    }

    [Fact]
    public async Task CheckForRelistingAsync_WithActiveMatchingVin_ShouldReturnNotRelisted()
    {
        // Arrange - create an ACTIVE listing
        var vin = "1HGBH41JXMN109186";
        var existingListing = Listing.Create(
            _testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, vin: vin);
        // Listing is active by default

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "TestSite",
            Vin = vin
        };

        // Act
        var result = await _service.CheckForRelistingAsync(scrapedInfo);

        // Assert - should not be considered relisting since previous is still active
        Assert.False(result.IsRelisted);
    }

    [Fact]
    public async Task CheckForRelistingAsync_WithDeactivatedMatchingVin_ShouldReturnRelisted()
    {
        // Arrange - create a DEACTIVATED listing
        var vin = "1HGBH41JXMN109186";
        var existingListing = Listing.Create(
            _testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, vin: vin);
        existingListing.Deactivate();

        // Ensure it's been off market long enough to be considered a relisting
        typeof(Listing)
            .GetProperty(nameof(Listing.DeactivatedAt))!
            .SetValue(existingListing, DateTime.UtcNow.AddDays(-2));

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "TestSite",
            Vin = vin,
            Price = 24000m
        };

        // Act
        var result = await _service.CheckForRelistingAsync(scrapedInfo);

        // Assert
        Assert.True(result.IsRelisted);
        Assert.NotNull(result.PreviousListing);
        Assert.Equal(existingListing.ListingId.Value, result.PreviousListingId);
        Assert.Equal(-1000m, result.PriceDelta);
        Assert.Equal(100.0, result.MatchConfidence);
    }

    [Fact]
    public async Task CheckForRelistingAsync_WithDeactivatedMatchingExternalId_ShouldReturnRelisted()
    {
        // Arrange - create a DEACTIVATED listing
        var externalId = "EXT-001";
        var sourceSite = "TestSite";
        var existingListing = Listing.Create(
            _testTenantId, externalId, sourceSite, "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used);
        existingListing.Deactivate();

        // Ensure it's been off market long enough to be considered a relisting
        typeof(Listing)
            .GetProperty(nameof(Listing.DeactivatedAt))!
            .SetValue(existingListing, DateTime.UtcNow.AddDays(-2));

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = externalId,
            SourceSite = sourceSite,
            Price = 26000m
        };

        // Act
        var result = await _service.CheckForRelistingAsync(scrapedInfo);

        // Assert
        Assert.True(result.IsRelisted);
        Assert.Equal(1000m, result.PriceDelta);
        Assert.Equal(95.0, result.MatchConfidence); // ExternalId match confidence
    }

    [Fact]
    public async Task CheckForRelistingAsync_WithDifferentTenant_ShouldReturnNotRelisted()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var vin = "1HGBH41JXMN109186";

        var existingListing = Listing.Create(
            otherTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, vin: vin);
        existingListing.Deactivate();

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId, // Different tenant
            ExternalId = "EXT-002",
            SourceSite = "TestSite",
            Vin = vin
        };

        // Act
        var result = await _service.CheckForRelistingAsync(scrapedInfo);

        // Assert
        Assert.False(result.IsRelisted);
    }

    [Fact]
    public async Task CheckForRelistingAsync_WithNullScrapedListing_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CheckForRelistingAsync(null!));
    }

    [Fact]
    public async Task GetStatsAsync_WithNoRelistedListings_ShouldReturnEmptyStats()
    {
        // Act
        var stats = await _service.GetStatsAsync(_testTenantId);

        // Assert
        Assert.Equal(0, stats.TotalRelistedCount);
        Assert.Equal(0, stats.ActiveRelistedCount);
        Assert.Equal(0, stats.FrequentRelisterCount);
    }

    [Fact]
    public async Task GetRelistedListingsAsync_WithNoRelistedListings_ShouldReturnEmptyList()
    {
        // Act
        var listings = await _service.GetRelistedListingsAsync(_testTenantId);

        // Assert
        Assert.Empty(listings);
    }

    [Fact]
    public void RelistingCheckResult_NotRelisted_ShouldHaveCorrectProperties()
    {
        // Act
        var result = RelistingCheckResult.NotRelisted();

        // Assert
        Assert.False(result.IsRelisted);
        Assert.Null(result.PreviousListing);
        Assert.Null(result.PreviousListingId);
        Assert.Null(result.TimeOffMarket);
        Assert.Null(result.PriceDelta);
    }

    [Fact]
    public void RelistingCheckResult_Relisted_ShouldCalculatePriceChangePercentage()
    {
        // Arrange
        var listing = Listing.Create(
            _testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 20000m, Condition.Used);
        listing.Deactivate();

        // Act
        var result = RelistingCheckResult.Relisted(
            listing,
            TimeSpan.FromDays(30),
            -2000m, // Price dropped by 2000
            95.0);

        // Assert
        Assert.True(result.IsRelisted);
        Assert.Equal(-2000m, result.PriceDelta);
        Assert.Equal(-10.0, result.PriceChangePercentage); // -2000/20000 * 100 = -10%
        Assert.Equal(1, result.TotalRelistCount);
    }

    private class TestContextWithRelisted : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContextWithRelisted(DbContextOptions<TestContextWithRelisted> options) : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Core.Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Core.Models.PriceHistoryAggregate.PriceHistory>();
        public DbSet<Core.Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Core.Models.SearchSessionAggregate.SearchSession>();
        public DbSet<Core.Models.SearchProfileAggregate.SearchProfile> SearchProfiles => Set<Core.Models.SearchProfileAggregate.SearchProfile>();
        public DbSet<Core.Models.VehicleAggregate.Vehicle> Vehicles => Set<Core.Models.VehicleAggregate.Vehicle>();
        public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
        public DbSet<Core.Models.WatchListAggregate.WatchedListing> WatchedListings => Set<Core.Models.WatchListAggregate.WatchedListing>();
        public DbSet<Core.Models.AlertAggregate.Alert> Alerts => Set<Core.Models.AlertAggregate.Alert>();
        public DbSet<Core.Models.AlertAggregate.AlertNotification> AlertNotifications => Set<Core.Models.AlertAggregate.AlertNotification>();
        public DbSet<Core.Models.DealerAggregate.Dealer> Dealers => Set<Core.Models.DealerAggregate.Dealer>();
        public DbSet<Core.Models.ScraperHealthAggregate.ScraperHealthRecord> ScraperHealthRecords => Set<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>();
        public DbSet<Core.Models.CacheAggregate.ResponseCacheEntry> ResponseCacheEntries => Set<Core.Models.CacheAggregate.ResponseCacheEntry>();
        public DbSet<Core.Models.ReportAggregate.Report> Reports => Set<Core.Models.ReportAggregate.Report>();
        public DbSet<Core.Models.DeduplicationAuditAggregate.AuditEntry> AuditEntries => Set<Core.Models.DeduplicationAuditAggregate.AuditEntry>();
        public DbSet<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig> DeduplicationConfigs => Set<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig>();
        public DbSet<Core.Models.CustomMarketAggregate.CustomMarket> CustomMarkets => Set<Core.Models.CustomMarketAggregate.CustomMarket>();
        public DbSet<Core.Models.ScheduledReportAggregate.ScheduledReport> ScheduledReports => Set<Core.Models.ScheduledReportAggregate.ScheduledReport>();
        public DbSet<Core.Models.ResourceThrottleAggregate.ResourceThrottle> ResourceThrottles => Set<Core.Models.ResourceThrottleAggregate.ResourceThrottle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<ReviewItem>(entity =>
            {
                entity.HasKey(r => r.ReviewItemId);
                entity.Property(r => r.ReviewItemId)
                    .HasConversion(id => id.Value, value => new ReviewItemId(value));
                entity.Property(r => r.Listing1Id)
                    .HasConversion(id => id.Value, value => new ListingId(value));
                entity.Property(r => r.Listing2Id)
                    .HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(r => r.DomainEvents);
            });

            modelBuilder.Entity<Core.Models.PriceHistoryAggregate.PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                entity.Property(ph => ph.PriceHistoryId)
                    .HasConversion(id => id.Value, value => new Core.Models.PriceHistoryAggregate.PriceHistoryId(value));
                entity.Property(ph => ph.ListingId)
                    .HasConversion(id => id.Value, value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.CustomMarketAggregate.CustomMarket>(entity =>
            {
                entity.HasKey(cm => cm.CustomMarketId);
                entity.Property(cm => cm.CustomMarketId).HasConversion(id => id.Value, value => new Core.Models.CustomMarketAggregate.CustomMarketId(value));
            });

            modelBuilder.Entity<Core.Models.ScheduledReportAggregate.ScheduledReport>(entity =>
            {
                entity.HasKey(sr => sr.ScheduledReportId);
                entity.Property(sr => sr.ScheduledReportId).HasConversion(id => id.Value, value => new Core.Models.ScheduledReportAggregate.ScheduledReportId(value));
            });

            modelBuilder.Entity<Core.Models.ResourceThrottleAggregate.ResourceThrottle>(entity =>
            {
                entity.HasKey(rt => rt.ResourceThrottleId);
                entity.Property(rt => rt.ResourceThrottleId).HasConversion(id => id.Value, value => new Core.Models.ResourceThrottleAggregate.ResourceThrottleId(value));
            });

            modelBuilder.Entity<Core.Models.DeduplicationAuditAggregate.AuditEntry>(entity =>
            {
                entity.HasKey(ae => ae.AuditEntryId);
                entity.Property(ae => ae.AuditEntryId).HasConversion(id => id.Value, value => new Core.Models.DeduplicationAuditAggregate.AuditEntryId(value));
            });

            modelBuilder.Entity<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig>(entity =>
            {
                entity.HasKey(dc => dc.ConfigId);
                entity.Property(dc => dc.ConfigId).HasConversion(id => id.Value, value => new Core.Models.DeduplicationConfigAggregate.DeduplicationConfigId(value));
            });

            modelBuilder.Entity<Core.Models.ReportAggregate.Report>(entity =>
            {
                entity.HasKey(r => r.ReportId);
                entity.Property(r => r.ReportId).HasConversion(id => id.Value, value => new Core.Models.ReportAggregate.ReportId(value));
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
                    .HasConversion(
                        id => id.Value,
                        value => Core.Models.SearchProfileAggregate.SearchProfileId.From(value));
            });

            modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId)
                    .HasConversion(id => id.Value, value => new Core.Models.VehicleAggregate.VehicleId(value));
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
        }
    }
}
