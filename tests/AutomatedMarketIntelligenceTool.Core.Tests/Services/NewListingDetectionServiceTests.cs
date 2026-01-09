using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class NewListingDetectionServiceTests
{
    private readonly NewListingDetectionService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public NewListingDetectionServiceTests()
    {
        _service = new NewListingDetectionService(NullLogger<NewListingDetectionService>.Instance);
    }

    [Fact]
    public async Task DetectNewListingsAsync_WithNoListings_ShouldReturnZero()
    {
        // Arrange
        var listings = Array.Empty<Listing>();

        // Act
        var result = await _service.DetectNewListingsAsync(listings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalListings);
        Assert.Equal(0, result.NewListingsCount);
        Assert.Empty(result.NewListingIds);
    }

    [Fact]
    public async Task DetectNewListingsAsync_WithAllNewListings_ShouldReturnAllAsNew()
    {
        // Arrange
        var listings = new[]
        {
            Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
                "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000),
            Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
                "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000),
            Listing.Create(_testTenantId, "EXT-003", "TestSite", "https://test.com/3",
                "Ford", "F-150", 2019, 35000m, Condition.Used, mileage: 45000)
        };

        // Act
        var result = await _service.DetectNewListingsAsync(listings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalListings);
        Assert.Equal(3, result.NewListingsCount);
        Assert.Equal(3, result.NewListingIds.Count);
    }

    [Fact]
    public async Task DetectNewListingsAsync_WithMixedListings_ShouldReturnOnlyNew()
    {
        // Arrange
        var newListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        
        var oldListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        oldListing.MarkAsSeen(); // This should set IsNewListing to false

        var listings = new[] { newListing, oldListing };

        // Act
        var result = await _service.DetectNewListingsAsync(listings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalListings);
        Assert.Equal(1, result.NewListingsCount);
        Assert.Single(result.NewListingIds);
        Assert.Contains(newListing.ListingId, result.NewListingIds);
    }

    [Fact]
    public async Task DetectNewListingsAsync_WithNullListings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.DetectNewListingsAsync(null!));
    }

    [Fact]
    public async Task DetectNewListingsAsync_WithMultipleNewListings_ShouldReturnAllNewIds()
    {
        // Arrange
        var listing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        
        var listing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);

        var listing3 = Listing.Create(_testTenantId, "EXT-003", "TestSite", "https://test.com/3",
            "Ford", "F-150", 2019, 35000m, Condition.Used, mileage: 45000);

        var listings = new[] { listing1, listing2, listing3 };

        // Act
        var result = await _service.DetectNewListingsAsync(listings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.NewListingsCount);
        Assert.Contains(listing1.ListingId, result.NewListingIds);
        Assert.Contains(listing2.ListingId, result.NewListingIds);
        Assert.Contains(listing3.ListingId, result.NewListingIds);
    }
}
