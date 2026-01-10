using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class PriceChangeDetectionServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly PriceChangeDetectionService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public PriceChangeDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new PriceChangeDetectionService(_context, NullLogger<PriceChangeDetectionService>.Instance);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithNoListings_ShouldReturnZero()
    {
        // Arrange
        var listings = Array.Empty<Listing>();

        // Act
        var result = await _service.DetectAndRecordPriceChangesAsync(_testTenantId, listings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalListings);
        Assert.Equal(0, result.PriceChangesCount);
        Assert.Empty(result.PriceChanges);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithNewListings_ShouldDetectNoPriceChanges()
    {
        // Arrange
        var listings = new[]
        {
            Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
                "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000),
            Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
                "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000)
        };

        // Act
        var result = await _service.DetectAndRecordPriceChangesAsync(_testTenantId, listings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalListings);
        Assert.Equal(0, result.PriceChangesCount);
        Assert.Empty(result.PriceChanges);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithPriceChange_ShouldDetectAndRecord()
    {
        // Arrange
        var originalListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        _context.Listings.Add(originalListing);
        await _context.SaveChangesAsync();

        var updatedListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Toyota", "Camry", 2020, 23000m, Condition.Used, mileage: 30000);
        
        // Use the same ListingId
        var listingIdField = typeof(Listing).GetProperty("ListingId")!;
        listingIdField.SetValue(updatedListing, originalListing.ListingId);

        // Act
        var result = await _service.DetectAndRecordPriceChangesAsync(_testTenantId, new[] { updatedListing });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalListings);
        Assert.Equal(1, result.PriceChangesCount);
        Assert.Single(result.PriceChanges);
        
        var priceChange = result.PriceChanges[0];
        Assert.Equal(originalListing.ListingId, priceChange.ListingId);
        Assert.Equal(25000m, priceChange.OldPrice);
        Assert.Equal(23000m, priceChange.NewPrice);
        Assert.Equal(-2000m, priceChange.PriceChange);
        Assert.NotNull(priceChange.ChangePercentage);
        Assert.Equal(-8.00m, priceChange.ChangePercentage!.Value);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithPriceIncrease_ShouldRecordPositiveChange()
    {
        // Arrange
        var originalListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        _context.Listings.Add(originalListing);
        await _context.SaveChangesAsync();

        var updatedListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 24000m, Condition.Used, mileage: 15000);
        
        var listingIdField = typeof(Listing).GetProperty("ListingId")!;
        listingIdField.SetValue(updatedListing, originalListing.ListingId);

        // Act
        var result = await _service.DetectAndRecordPriceChangesAsync(_testTenantId, new[] { updatedListing });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PriceChangesCount);
        
        var priceChange = result.PriceChanges[0];
        Assert.Equal(22000m, priceChange.OldPrice);
        Assert.Equal(24000m, priceChange.NewPrice);
        Assert.Equal(2000m, priceChange.PriceChange);
        Assert.True(priceChange.ChangePercentage > 0);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithNoPriceChange_ShouldNotRecordHistory()
    {
        // Arrange
        var originalListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        _context.Listings.Add(originalListing);
        await _context.SaveChangesAsync();

        var samePrice = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        
        var listingIdField = typeof(Listing).GetProperty("ListingId")!;
        listingIdField.SetValue(samePrice, originalListing.ListingId);

        // Act
        var result = await _service.DetectAndRecordPriceChangesAsync(_testTenantId, new[] { samePrice });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalListings);
        Assert.Equal(0, result.PriceChangesCount);
        Assert.Empty(result.PriceChanges);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithMultiplePriceChanges_ShouldRecordAll()
    {
        // Arrange
        var listing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var listing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        
        _context.Listings.AddRange(listing1, listing2);
        await _context.SaveChangesAsync();

        var updated1 = Listing.Create(_testTenantId, "EXT-003", "TestSite", "https://test.com/3",
            "Toyota", "Camry", 2020, 24000m, Condition.Used, mileage: 30000);
        var updated2 = Listing.Create(_testTenantId, "EXT-004", "TestSite", "https://test.com/4",
            "Honda", "Civic", 2021, 21000m, Condition.Used, mileage: 15000);

        var listingIdField = typeof(Listing).GetProperty("ListingId")!;
        listingIdField.SetValue(updated1, listing1.ListingId);
        listingIdField.SetValue(updated2, listing2.ListingId);

        // Act
        var result = await _service.DetectAndRecordPriceChangesAsync(_testTenantId, new[] { updated1, updated2 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalListings);
        Assert.Equal(2, result.PriceChangesCount);
        Assert.Equal(2, result.PriceChanges.Count);
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_WithNullListings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.DetectAndRecordPriceChangesAsync(_testTenantId, null!));
    }

    [Fact]
    public async Task DetectAndRecordPriceChangesAsync_ShouldCreatePriceHistoryRecord()
    {
        // Arrange
        var originalListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        _context.Listings.Add(originalListing);
        await _context.SaveChangesAsync();

        var updatedListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Toyota", "Camry", 2020, 23000m, Condition.Used, mileage: 30000);
        
        var listingIdField = typeof(Listing).GetProperty("ListingId")!;
        listingIdField.SetValue(updatedListing, originalListing.ListingId);

        // Act
        await _service.DetectAndRecordPriceChangesAsync(_testTenantId, new[] { updatedListing });

        // Assert
        var priceHistoryRecords = await _context.PriceHistory
            .Where(ph => ph.ListingId == originalListing.ListingId)
            .ToListAsync();
        
        Assert.Single(priceHistoryRecords);
        var record = priceHistoryRecords[0];
        Assert.Equal(_testTenantId, record.TenantId);
        Assert.Equal(23000m, record.Price);
        Assert.Equal(-2000m, record.PriceChange);
    }

    private class TestContext : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
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
                    .HasConversion(
                        id => id.Value,
                        value => new ListingId(value));

                entity.Ignore(l => l.DomainEvents);
                entity.Ignore(l => l.Location);
                entity.Ignore(l => l.Dealer);
                entity.Ignore(l => l.DealerEntity);
                entity.Ignore(l => l.DealerId);
            });

            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                
                entity.Property(ph => ph.PriceHistoryId)
                    .HasConversion(
                        id => id.Value,
                        value => new PriceHistoryId(value));

                entity.Property(ph => ph.ListingId)
                    .HasConversion(
                        id => id.Value,
                        value => new ListingId(value));
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
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.SearchSessionAggregate.SearchSessionId(value));
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
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.VehicleAggregate.VehicleId(value));
            });

            modelBuilder.Entity<Core.Models.ReviewQueueAggregate.ReviewItem>(entity =>
            {
                entity.HasKey(r => r.ReviewItemId);
                entity.Property(r => r.ReviewItemId)
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.ReviewQueueAggregate.ReviewItemId(value));

                entity.Property(r => r.Listing1Id)
                    .HasConversion(id => id.Value, value => new ListingId(value));

                entity.Property(r => r.Listing2Id)
                    .HasConversion(id => id.Value, value => new ListingId(value));

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
        }
    }
}
