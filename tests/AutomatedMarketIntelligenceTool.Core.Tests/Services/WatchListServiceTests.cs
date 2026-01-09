using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class WatchListServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly WatchListService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public WatchListServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new WatchListService(_context, NullLogger<WatchListService>.Instance);
    }

    [Fact]
    public async Task AddToWatchListAsync_WithValidData_ShouldCreateWatchedListing()
    {
        // Arrange
        var listing = CreateAndSaveListing("EXT-001");

        // Act
        var watchedListing = await _service.AddToWatchListAsync(
            _testTenantId,
            listing.ListingId,
            "Great deal");

        // Assert
        Assert.NotNull(watchedListing);
        Assert.Equal(_testTenantId, watchedListing.TenantId);
        Assert.Equal(listing.ListingId, watchedListing.ListingId);
        Assert.Equal("Great deal", watchedListing.Notes);
    }

    [Fact]
    public async Task AddToWatchListAsync_WithDuplicateListing_ShouldReturnExisting()
    {
        // Arrange
        var listing = CreateAndSaveListing("EXT-001");
        var first = await _service.AddToWatchListAsync(_testTenantId, listing.ListingId);

        // Act
        var second = await _service.AddToWatchListAsync(_testTenantId, listing.ListingId);

        // Assert
        Assert.Equal(first.WatchedListingId, second.WatchedListingId);
    }

    [Fact]
    public async Task AddToWatchListAsync_WithInvalidListing_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidId = new ListingId(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddToWatchListAsync(_testTenantId, invalidId));
    }

    [Fact]
    public async Task RemoveFromWatchListAsync_ShouldRemoveWatchedListing()
    {
        // Arrange
        var listing = CreateAndSaveListing("EXT-001");
        await _service.AddToWatchListAsync(_testTenantId, listing.ListingId);

        // Act
        await _service.RemoveFromWatchListAsync(_testTenantId, listing.ListingId);

        // Assert
        var exists = await _service.IsWatchedAsync(_testTenantId, listing.ListingId);
        Assert.False(exists);
    }

    [Fact]
    public async Task GetAllWatchedListingsAsync_ShouldReturnAllWatchedListings()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");
        await _service.AddToWatchListAsync(_testTenantId, listing1.ListingId);
        await _service.AddToWatchListAsync(_testTenantId, listing2.ListingId);

        // Act
        var watchedListings = await _service.GetAllWatchedListingsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, watchedListings.Count);
    }

    [Fact]
    public async Task UpdateNotesAsync_ShouldUpdateNotes()
    {
        // Arrange
        var listing = CreateAndSaveListing("EXT-001");
        await _service.AddToWatchListAsync(_testTenantId, listing.ListingId, "Initial notes");

        // Act
        await _service.UpdateNotesAsync(_testTenantId, listing.ListingId, "Updated notes");

        // Assert
        var watchedListing = await _service.GetWatchedListingAsync(_testTenantId, listing.ListingId);
        Assert.Equal("Updated notes", watchedListing!.Notes);
    }

    [Fact]
    public async Task IsWatchedAsync_WithWatchedListing_ShouldReturnTrue()
    {
        // Arrange
        var listing = CreateAndSaveListing("EXT-001");
        await _service.AddToWatchListAsync(_testTenantId, listing.ListingId);

        // Act
        var isWatched = await _service.IsWatchedAsync(_testTenantId, listing.ListingId);

        // Assert
        Assert.True(isWatched);
    }

    [Fact]
    public async Task IsWatchedAsync_WithUnwatchedListing_ShouldReturnFalse()
    {
        // Arrange
        var listing = CreateAndSaveListing("EXT-001");

        // Act
        var isWatched = await _service.IsWatchedAsync(_testTenantId, listing.ListingId);

        // Assert
        Assert.False(isWatched);
    }

    private Listing CreateAndSaveListing(string externalId)
    {
        var listing = Listing.Create(
            tenantId: _testTenantId,
            externalId: externalId,
            sourceSite: "test-site",
            listingUrl: "https://example.com/listing",
            make: "Toyota",
            model: "Camry",
            year: 2022,
            price: 25000m,
            condition: Condition.Used);

        _context.Listings.Add(listing);
        _context.SaveChangesAsync().GetAwaiter().GetResult();
        return listing;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ignore strongly-typed ID value objects
            modelBuilder.Ignore<ListingId>();
            modelBuilder.Ignore<Core.Models.WatchListAggregate.WatchedListingId>();
            modelBuilder.Ignore<Core.Models.AlertAggregate.AlertId>();
            modelBuilder.Ignore<Core.Models.DealerAggregate.DealerId>();
            modelBuilder.Ignore<Core.Models.ScraperHealthAggregate.ScraperHealthRecordId>();
            modelBuilder.Ignore<Core.Models.VehicleAggregate.VehicleId>();
            modelBuilder.Ignore<Core.Models.PriceHistoryAggregate.PriceHistoryId>();
            modelBuilder.Ignore<Core.Models.SearchSessionAggregate.SearchSessionId>();
            modelBuilder.Ignore<Core.Models.SearchProfileAggregate.SearchProfileId>();
            modelBuilder.Ignore<Core.Models.ReviewQueueAggregate.ReviewItemId>();

            modelBuilder.Entity<Listing>(entity =>
            {
                entity.HasKey(l => l.ListingId);
                entity.Property(l => l.ListingId).HasConversion(
                    id => id.Value,
                    value => new ListingId(value));
                entity.Ignore(l => l.DomainEvents);
                entity.Ignore(l => l.Location);
                entity.Ignore(l => l.Dealer);
                entity.Ignore(l => l.DealerId);
            });

            modelBuilder.Entity<WatchedListing>(entity =>
            {
                entity.HasKey(w => w.WatchedListingId);
                entity.Property(w => w.WatchedListingId).HasConversion(
                    id => id.Value,
                    value => new WatchedListingId(value));
                entity.Property(w => w.ListingId).HasConversion(
                    id => id.Value,
                    value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.AlertAggregate.Alert>(entity =>
            {
                entity.HasKey(a => a.AlertId);
                entity.Property(a => a.AlertId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.AlertAggregate.AlertId(value));
            });

            modelBuilder.Entity<Core.Models.AlertAggregate.AlertNotification>(entity =>
            {
                entity.HasKey(an => an.NotificationId);
                entity.Property(an => an.AlertId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.AlertAggregate.AlertId(value));
                entity.Property(an => an.ListingId).HasConversion(
                    id => id.Value,
                    value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.DealerAggregate.Dealer>(entity =>
            {
                entity.HasKey(d => d.DealerId);
                entity.Property(d => d.DealerId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.DealerAggregate.DealerId(value));
            });

            modelBuilder.Entity<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>(entity =>
            {
                entity.HasKey(sh => sh.ScraperHealthRecordId);
                entity.Property(sh => sh.ScraperHealthRecordId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.ScraperHealthAggregate.ScraperHealthRecordId(value));
            });

            modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId).HasConversion(id => id.Value, value => new Core.Models.VehicleAggregate.VehicleId(value));
            });

            modelBuilder.Entity<Core.Models.PriceHistoryAggregate.PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                entity.Property(ph => ph.PriceHistoryId).HasConversion(id => id.Value, value => new Core.Models.PriceHistoryAggregate.PriceHistoryId(value));
                entity.Property(ph => ph.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.SearchSessionAggregate.SearchSession>(entity =>
            {
                entity.HasKey(ss => ss.SearchSessionId);
                entity.Property(ss => ss.SearchSessionId).HasConversion(id => id.Value, value => new Core.Models.SearchSessionAggregate.SearchSessionId(value));
            });

            modelBuilder.Entity<Core.Models.SearchProfileAggregate.SearchProfile>(entity =>
            {
                entity.HasKey(sp => sp.SearchProfileId);
                entity.Property(sp => sp.SearchProfileId).HasConversion(id => id.Value, value => Core.Models.SearchProfileAggregate.SearchProfileId.From(value));
            });

            modelBuilder.Entity<Core.Models.ReviewQueueAggregate.ReviewItem>(entity =>
            {
                entity.HasKey(ri => ri.ReviewItemId);
                entity.Property(ri => ri.ReviewItemId).HasConversion(id => id.Value, value => new Core.Models.ReviewQueueAggregate.ReviewItemId(value));
                entity.Property(ri => ri.Listing1Id).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Property(ri => ri.Listing2Id).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(ri => ri.DomainEvents);
            });
        }
    }
}
