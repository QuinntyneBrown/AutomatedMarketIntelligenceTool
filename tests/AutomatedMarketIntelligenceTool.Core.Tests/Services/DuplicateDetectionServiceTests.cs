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

            // Configure SearchSession entity
            modelBuilder.Entity<Core.Models.SearchSessionAggregate.SearchSession>(entity =>
            {
                entity.HasKey(ss => ss.SearchSessionId);
                
                entity.Property(ss => ss.SearchSessionId)
                    .HasConversion(
                        id => id.Value,
                        value => new Core.Models.SearchSessionAggregate.SearchSessionId(value));
            });
        }
    }
}
