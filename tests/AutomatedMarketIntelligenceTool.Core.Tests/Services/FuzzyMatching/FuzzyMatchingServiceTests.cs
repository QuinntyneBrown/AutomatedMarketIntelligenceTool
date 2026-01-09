using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.FuzzyMatching;

public class FuzzyMatchingServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly FuzzyMatchingService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public FuzzyMatchingServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);

        var levenshtein = new LevenshteinCalculator();
        var numeric = new NumericProximityCalculator();
        var geo = new GeoDistanceCalculator();
        var confidenceCalculator = new ConfidenceScoreCalculator(levenshtein, numeric, geo);

        _service = new FuzzyMatchingService(
            _context,
            confidenceCalculator,
            NullLogger<FuzzyMatchingService>.Instance);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithExactVinMatch_ShouldReturnVinMatch()
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

        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "AnotherSite",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 25500m,
            Vin = vin
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert
        Assert.NotNull(result.MatchedListing);
        Assert.Equal(100m, result.ConfidenceScore);
        Assert.Equal(MatchMethod.ExactVin, result.Method);
        Assert.Equal(existingListing.ListingId.Value, result.MatchedListing!.ListingId.Value);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithPartialVinMatch_ShouldReturnPartialVinMatch()
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

        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "AnotherSite",
            Make = "Honda",
            Model = "Civic",
            Year = 2021,
            Price = 22500m,
            Vin = "ABCDE41JXMN109186" // Last 8 characters match
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert
        Assert.NotNull(result.MatchedListing);
        Assert.Equal(95m, result.ConfidenceScore);
        Assert.Equal(MatchMethod.PartialVin, result.Method);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithExternalIdMatch_ShouldReturnExternalIdMatch()
    {
        // Arrange
        var existingListing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Ford",
            "F-150",
            2022,
            35000m,
            Condition.Used);

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-001",
            SourceSite = "TestSite",
            Make = "Ford",
            Model = "F-150",
            Year = 2022,
            Price = 35500m
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert
        Assert.NotNull(result.MatchedListing);
        Assert.Equal(100m, result.ConfidenceScore);
        Assert.Equal(MatchMethod.ExternalId, result.Method);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithHighConfidenceFuzzyMatch_ShouldReturnFuzzyMatch()
    {
        // Arrange
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
            mileage: 50000);

        existingListing.SetLocation(43.6532m, -79.3832m);

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "AnotherSite",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 25200m,
            Mileage = 50300,
            Latitude = 43.6540m,
            Longitude = -79.3840m
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert
        Assert.NotNull(result.MatchedListing);
        Assert.True(result.ConfidenceScore >= 85m);
        Assert.Equal(MatchMethod.FuzzyAttributes, result.Method);
        Assert.NotEmpty(result.FieldScores);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithNoMatch_ShouldReturnNone()
    {
        // Arrange
        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-NEW",
            SourceSite = "TestSite",
            Make = "Tesla",
            Model = "Model 3",
            Year = 2023,
            Price = 45000m,
            Vin = "5YJ3E1EA0KF999999"
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert
        Assert.Null(result.MatchedListing);
        Assert.Equal(0m, result.ConfidenceScore);
        Assert.Equal(MatchMethod.None, result.Method);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithLowConfidenceFuzzyMatch_ShouldReturnNone()
    {
        // Arrange
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
            mileage: 50000);

        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-002",
            SourceSite = "AnotherSite",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 35000m, // Very different price
            Mileage = 100000 // Very different mileage
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert - Low confidence should not return a match
        Assert.Null(result.MatchedListing);
    }

    [Fact]
    public async Task FindBestMatchAsync_WithNullListing_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.FindBestMatchAsync(null!));
    }

    [Fact]
    public async Task FindBestMatchAsync_WithVinPriorityOverExternalId_ShouldReturnVinMatch()
    {
        // Arrange
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

        var scrapedListing = new ScrapedListingData
        {
            TenantId = _testTenantId,
            ExternalId = "EXT-001",
            SourceSite = "TestSite",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 25000m,
            Vin = "1HGBH41JXMN109186"
        };

        // Act
        var result = await _service.FindBestMatchAsync(scrapedListing);

        // Assert - VIN match should take priority
        Assert.NotNull(result.MatchedListing);
        Assert.Equal(MatchMethod.ExactVin, result.Method);
        Assert.Equal(vinMatchListing.ListingId.Value, result.MatchedListing!.ListingId.Value);
    }

    private class TestContext : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Core.Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Core.Models.PriceHistoryAggregate.PriceHistory>();
        public DbSet<Core.Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Core.Models.SearchSessionAggregate.SearchSession>();
        public DbSet<Core.Models.VehicleAggregate.Vehicle> Vehicles => Set<Core.Models.VehicleAggregate.Vehicle>();

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

            modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId)
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.VehicleAggregate.VehicleId(value));
            });
        }
    }
}
