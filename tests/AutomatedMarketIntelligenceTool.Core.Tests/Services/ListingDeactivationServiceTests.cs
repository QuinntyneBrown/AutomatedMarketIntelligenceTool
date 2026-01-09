using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class ListingDeactivationServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ListingDeactivationService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ListingDeactivationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new ListingDeactivationService(_context, NullLogger<ListingDeactivationService>.Instance);
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithNoListings_ShouldReturnZero()
    {
        // Act
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalActiveListings);
        Assert.Equal(0, result.DeactivatedCount);
        Assert.Empty(result.DeactivatedListingIds);
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithRecentListings_ShouldNotDeactivate()
    {
        // Arrange
        var listing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var listing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        
        _context.Listings.AddRange(listing1, listing2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalActiveListings);
        Assert.Equal(0, result.DeactivatedCount);
        Assert.Empty(result.DeactivatedListingIds);
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithStaleListings_ShouldDeactivate()
    {
        // Arrange
        var staleListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        
        // Set LastSeenDate to 10 days ago using reflection
        var lastSeenField = typeof(Listing).GetProperty("LastSeenDate")!;
        lastSeenField.SetValue(staleListing, DateTime.UtcNow.AddDays(-10));

        var recentListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        
        _context.Listings.AddRange(staleListing, recentListing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalActiveListings);
        Assert.Equal(1, result.DeactivatedCount);
        Assert.Single(result.DeactivatedListingIds);
        Assert.Contains(staleListing.ListingId, result.DeactivatedListingIds);
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithMultipleStaleListings_ShouldDeactivateAll()
    {
        // Arrange
        var staleListing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var staleListing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        var staleListing3 = Listing.Create(_testTenantId, "EXT-003", "TestSite", "https://test.com/3",
            "Ford", "F-150", 2019, 35000m, Condition.Used, mileage: 45000);
        
        var lastSeenField = typeof(Listing).GetProperty("LastSeenDate")!;
        lastSeenField.SetValue(staleListing1, DateTime.UtcNow.AddDays(-10));
        lastSeenField.SetValue(staleListing2, DateTime.UtcNow.AddDays(-8));
        lastSeenField.SetValue(staleListing3, DateTime.UtcNow.AddDays(-15));

        _context.Listings.AddRange(staleListing1, staleListing2, staleListing3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalActiveListings);
        Assert.Equal(3, result.DeactivatedCount);
        Assert.Equal(3, result.DeactivatedListingIds.Count);
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithCustomStaleDays_ShouldRespectThreshold()
    {
        // Arrange
        var listing5DaysOld = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var listing10DaysOld = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        
        var lastSeenField = typeof(Listing).GetProperty("LastSeenDate")!;
        lastSeenField.SetValue(listing5DaysOld, DateTime.UtcNow.AddDays(-5));
        lastSeenField.SetValue(listing10DaysOld, DateTime.UtcNow.AddDays(-10));

        _context.Listings.AddRange(listing5DaysOld, listing10DaysOld);
        await _context.SaveChangesAsync();

        // Act - Using 3 days as threshold
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalActiveListings);
        Assert.Equal(2, result.DeactivatedCount); // Both should be deactivated
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_ShouldOnlyAffectActiveListing()
    {
        // Arrange
        var staleListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        
        var lastSeenField = typeof(Listing).GetProperty("LastSeenDate")!;
        lastSeenField.SetValue(staleListing, DateTime.UtcNow.AddDays(-10));
        staleListing.Deactivate(); // Already deactivated

        var recentListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        
        _context.Listings.AddRange(staleListing, recentListing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalActiveListings); // Only one is active
        Assert.Equal(0, result.DeactivatedCount); // None deactivated because the stale one was already inactive
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_ShouldOnlyAffectSpecifiedTenant()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        
        var tenant1Listing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var tenant2Listing = Listing.Create(otherTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        
        var lastSeenField = typeof(Listing).GetProperty("LastSeenDate")!;
        lastSeenField.SetValue(tenant1Listing, DateTime.UtcNow.AddDays(-10));
        lastSeenField.SetValue(tenant2Listing, DateTime.UtcNow.AddDays(-10));

        _context.Listings.AddRange(tenant1Listing, tenant2Listing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalActiveListings);
        Assert.Equal(1, result.DeactivatedCount);
        Assert.Contains(tenant1Listing.ListingId, result.DeactivatedListingIds);
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithZeroStaleDays_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 0));
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_WithNegativeStaleDays_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: -1));
    }

    [Fact]
    public async Task DeactivateStaleListingsAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var staleListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        
        var lastSeenField = typeof(Listing).GetProperty("LastSeenDate")!;
        lastSeenField.SetValue(staleListing, DateTime.UtcNow.AddDays(-10));

        _context.Listings.Add(staleListing);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateStaleListingsAsync(_testTenantId, staleDays: 7);

        // Assert
        var deactivatedListing = await _context.Listings
            .FirstOrDefaultAsync(l => l.ListingId == staleListing.ListingId);
        
        Assert.NotNull(deactivatedListing);
        Assert.False(deactivatedListing.IsActive);
        Assert.NotNull(deactivatedListing.DeactivatedAt);
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
            });

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
        }
    }
}
