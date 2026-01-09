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
