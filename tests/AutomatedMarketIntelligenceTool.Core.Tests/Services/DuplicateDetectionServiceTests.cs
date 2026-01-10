using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class DuplicateDetectionServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly DuplicateDetectionService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DuplicateDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new DuplicateDetectionService(_context, NullLogger<DuplicateDetectionService>.Instance);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithMatchingVin_ShouldReturnVinMatch()
    {
        // Arrange
        var vin = "1HGBH41JXMN109186";
        var existingListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Toyota",
            "Camry",
            2020,
            25000m,
            Condition.Used,
            vin: vin);

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "AnotherSite",
            Vin = vin
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.VinMatch, result.MatchType);
        Assert.Equal(existingListing.ListingId.Value, result.ExistingListingId);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithMatchingVinCaseInsensitive_ShouldReturnVinMatch()
    {
        // Arrange
        var existingListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Honda",
            "Civic",
            2021,
            22000m,
            Condition.Used,
            vin: "1HGBH41JXMN109186");

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "TestSite",
            Vin = "1hgbh41jxmn109186" // lowercase
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.VinMatch, result.MatchType);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithInvalidVinLength_ShouldFallbackToExternalIdCheck()
    {
        // Arrange
        var existingListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Ford",
            "F-150",
            2019,
            30000m,
            Condition.Used,
            vin: "INVALID");

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-001",
            SourceSite = "TestSite",
            Vin = "INVALID" // Too short
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.ExternalIdMatch, result.MatchType);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithMatchingExternalIdAndSourceSite_ShouldReturnExternalIdMatch()
    {
        // Arrange
        var existingListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Tesla",
            "Model 3",
            2022,
            45000m,
            Condition.New);

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-001",
            SourceSite = "TestSite",
            Vin = null
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.ExternalIdMatch, result.MatchType);
        Assert.Equal(existingListing.ListingId.Value, result.ExistingListingId);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithSameExternalIdDifferentSourceSite_ShouldReturnNewListing()
    {
        // Arrange
        var existingListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "BMW",
            "X5",
            2021,
            55000m,
            Condition.Used);

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-001",
            SourceSite = "DifferentSite",
            Vin = null
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.None, result.MatchType);
        Assert.Null(result.ExistingListingId);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithNoMatch_ShouldReturnNewListing()
    {
        // Arrange
        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-NEW",
            SourceSite = "TestSite",
            Vin = "1HGBH41JXMN999999"
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.None, result.MatchType);
        Assert.Null(result.ExistingListingId);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithDifferentTenantId_ShouldReturnNewListing()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var existingListing = Listing.Create(
            otherTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Chevrolet",
            "Silverado",
            2020,
            35000m,
            Condition.Used,
            vin: "1HGBH41JXMN109186");

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId, // Different tenant
            ExternalId = "EXT-001",
            SourceSite = "TestSite",
            Vin = "1HGBH41JXMN109186"
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.None, result.MatchType);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithNullScrapedListing_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CheckForDuplicateAsync(null!));
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WithVinPriorityOverExternalId_ShouldReturnVinMatch()
    {
        // Arrange - Create two listings: one with matching VIN, one with matching ExternalId
        var vinMatchListing = Listing.Create(
            _testTenantId,
            "EXT-VIN",
            "TestSite",
            "https://test.com/1",
            "Toyota",
            "Camry",
            2020,
            25000m,
            Condition.Used,
            vin: "1HGBH41JXMN109186");

        var externalIdMatchListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/2",
            "Honda",
            "Civic",
            2021,
            22000m,
            Condition.Used);

        _context.Listings.Add(vinMatchListing);
        _context.Listings.Add(externalIdMatchListing);
        await _context.SaveChangesAsync();

        var scrapedInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-001", // Matches second listing
            SourceSite = "TestSite",
            Vin = "1HGBH41JXMN109186" // Matches first listing
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(scrapedInfo);

        // Assert - VIN match should take priority
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.VinMatch, result.MatchType);
        Assert.Equal(vinMatchListing.ListingId.Value, result.ExistingListingId);
    }

    [Fact]
    public void DuplicateCheckResult_VinMatch_ShouldHaveCorrectProperties()
    {
        // Arrange
        var listingId = Guid.NewGuid();

        // Act
        var result = DuplicateCheckResult.VinMatch(listingId);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.VinMatch, result.MatchType);
        Assert.Equal(listingId, result.ExistingListingId);
    }

    [Fact]
    public void DuplicateCheckResult_ExternalIdMatch_ShouldHaveCorrectProperties()
    {
        // Arrange
        var listingId = Guid.NewGuid();

        // Act
        var result = DuplicateCheckResult.ExternalIdMatch(listingId);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.ExternalIdMatch, result.MatchType);
        Assert.Equal(listingId, result.ExistingListingId);
    }

    [Fact]
    public void DuplicateCheckResult_NewListing_ShouldHaveCorrectProperties()
    {
        // Act
        var result = DuplicateCheckResult.NewListing();

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal(DuplicateMatchType.None, result.MatchType);
        Assert.Null(result.ExistingListingId);
    }

    private class TestContext : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options)
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
        public DbSet<Core.Models.DeduplicationAuditAggregate.AuditEntry> AuditEntries => Set<Core.Models.DeduplicationAuditAggregate.AuditEntry>();
        public DbSet<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig> DeduplicationConfigs => Set<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig>();
        public DbSet<Core.Models.CustomMarketAggregate.CustomMarket> CustomMarkets => Set<Core.Models.CustomMarketAggregate.CustomMarket>();
        public DbSet<Core.Models.ScheduledReportAggregate.ScheduledReport> ScheduledReports => Set<Core.Models.ScheduledReportAggregate.ScheduledReport>();
        public DbSet<Core.Models.ResourceThrottleAggregate.ResourceThrottle> ResourceThrottles => Set<Core.Models.ResourceThrottleAggregate.ResourceThrottle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Listing entity  
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

            // Configure PriceHistory entity
            modelBuilder.Entity<Core.Models.PriceHistoryAggregate.PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                
                entity.Property(ph => ph.PriceHistoryId)
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.PriceHistoryAggregate.PriceHistoryId(value));

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

            // Configure SearchSession entity
            modelBuilder.Entity<Core.Models.SearchSessionAggregate.SearchSession>(entity =>
            {
                entity.HasKey(ss => ss.SearchSessionId);
                
                entity.Property(ss => ss.SearchSessionId)
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.SearchSessionAggregate.SearchSessionId(value));
            });

            // Configure SearchProfile entity
            modelBuilder.Entity<Core.Models.SearchProfileAggregate.SearchProfile>(entity =>
            {
                entity.HasKey(sp => sp.SearchProfileId);
                
                entity.Property(sp => sp.SearchProfileId)
                    .HasConversion(
                        id => id.Value,
                        value => Core.Models.SearchProfileAggregate.SearchProfileId.From(value));
            });

            // Configure Vehicle entity
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
