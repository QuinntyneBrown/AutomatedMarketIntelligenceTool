using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests;

public class AutomatedMarketIntelligenceToolContextTests
{
    private static DbContextOptions<AutomatedMarketIntelligenceToolContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistListing()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new AutomatedMarketIntelligenceToolContext(options);

        var listing = Listing.Create(
            Guid.Empty, // Using Guid.Empty to match the DbContext tenant filter
            "EXT-001",
            "TestSite",
            "https://example.com/listing/001",
            "Toyota",
            "Corolla",
            2020,
            18000m,
            Condition.Used);

        // Act
        context.Listings.Add(listing);
        var result = await context.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Single(context.Listings);
    }

    [Fact]
    public async Task Listings_ShouldRetrievePersistedListing()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var tenantId = Guid.Empty; // Using Guid.Empty to match the DbContext tenant filter
        var listingId = ListingId.Create();

        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = Listing.Create(
                tenantId,
                "EXT-002",
                "TestSite",
                "https://example.com/listing/002",
                "Honda",
                "Accord",
                2019,
                22000m,
                Condition.Certified,
                mileage: 30000,
                vin: "1HGCM82633A123456");

            context.Listings.Add(listing);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var retrievedListings = await context.Listings.ToListAsync();

            // Assert
            Assert.Single(retrievedListings);
            var listing = retrievedListings.First();
            Assert.Equal("Honda", listing.Make);
            Assert.Equal("Accord", listing.Model);
            Assert.Equal(2019, listing.Year);
            Assert.Equal(22000m, listing.Price);
            Assert.Equal(30000, listing.Mileage);
            Assert.Equal("1HGCM82633A123456", listing.Vin);
        }
    }

    [Fact]
    public async Task Listings_ShouldPersistImageUrls()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var imageUrls = new List<string>
        {
            "https://example.com/image1.jpg",
            "https://example.com/image2.jpg",
            "https://example.com/image3.jpg"
        };

        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = Listing.Create(
                Guid.Empty, // Using Guid.Empty to match the DbContext tenant filter
                "EXT-003",
                "TestSite",
                "https://example.com/listing/003",
                "Ford",
                "Mustang",
                2021,
                35000m,
                Condition.New,
                imageUrls: imageUrls);

            context.Listings.Add(listing);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = await context.Listings.FirstAsync();

            // Assert
            Assert.Equal(3, listing.ImageUrls.Count);
            Assert.Contains("https://example.com/image1.jpg", listing.ImageUrls);
            Assert.Contains("https://example.com/image2.jpg", listing.ImageUrls);
            Assert.Contains("https://example.com/image3.jpg", listing.ImageUrls);
        }
    }

    [Fact]
    public async Task Listings_ShouldPersistEnumValues()
    {
        // Arrange
        var options = CreateInMemoryOptions();

        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = Listing.Create(
                Guid.Empty, // Using Guid.Empty to match the DbContext tenant filter
                "EXT-004",
                "TestSite",
                "https://example.com/listing/004",
                "BMW",
                "X5",
                2020,
                45000m,
                Condition.Certified,
                transmission: Transmission.Automatic,
                fuelType: FuelType.Hybrid);

            context.Listings.Add(listing);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = await context.Listings.FirstAsync();

            // Assert
            Assert.Equal(Condition.Certified, listing.Condition);
            Assert.Equal(Transmission.Automatic, listing.Transmission);
            Assert.Equal(FuelType.Hybrid, listing.FuelType);
        }
    }

    [Fact]
    public async Task Listings_ShouldUpdateExistingListing()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        ListingId listingId;

        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = Listing.Create(
                Guid.Empty, // Using Guid.Empty to match the DbContext tenant filter
                "EXT-005",
                "TestSite",
                "https://example.com/listing/005",
                "Tesla",
                "Model 3",
                2022,
                40000m,
                Condition.New);

            listingId = listing.ListingId;
            context.Listings.Add(listing);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = await context.Listings.FirstAsync(l => l.ListingId == listingId);
            listing.UpdatePrice(38000m);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = await context.Listings.FirstAsync(l => l.ListingId == listingId);
            Assert.Equal(38000m, listing.Price);
            Assert.NotNull(listing.UpdatedAt);
        }
    }

    [Fact]
    public async Task Listings_ShouldPersistListingIdAsValueObject()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var expectedGuid = Guid.NewGuid();
        ListingId listingId = expectedGuid;

        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = Listing.Create(
                Guid.Empty, // Using Guid.Empty to match the DbContext tenant filter
                "EXT-006",
                "TestSite",
                "https://example.com/listing/006",
                "Audi",
                "A4",
                2021,
                32000m,
                Condition.Used);

            // Override the generated ID for testing
            typeof(Listing).GetProperty(nameof(Listing.ListingId))!
                .SetValue(listing, listingId);

            context.Listings.Add(listing);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new AutomatedMarketIntelligenceToolContext(options))
        {
            var listing = await context.Listings.FirstAsync();

            // Assert
            Assert.Equal(expectedGuid, listing.ListingId.Value);
        }
    }
}
