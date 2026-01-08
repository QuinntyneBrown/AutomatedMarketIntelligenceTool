using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Models.ListingAggregate;

public class ListingTests
{
    [Fact]
    public void Create_ShouldCreateListingWithRequiredFields()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var externalId = "TEST-123";
        var sourceSite = "TestSite";
        var listingUrl = "https://example.com/listing/123";
        var make = "Toyota";
        var model = "Camry";
        var year = 2020;
        var price = 25000m;
        var condition = Condition.Used;

        // Act
        var listing = Listing.Create(
            tenantId,
            externalId,
            sourceSite,
            listingUrl,
            make,
            model,
            year,
            price,
            condition);

        // Assert
        Assert.NotNull(listing);
        Assert.NotEqual(Guid.Empty, listing.ListingId.Value);
        Assert.Equal(tenantId, listing.TenantId);
        Assert.Equal(externalId, listing.ExternalId);
        Assert.Equal(sourceSite, listing.SourceSite);
        Assert.Equal(listingUrl, listing.ListingUrl);
        Assert.Equal(make, listing.Make);
        Assert.Equal(model, listing.Model);
        Assert.Equal(year, listing.Year);
        Assert.Equal(price, listing.Price);
        Assert.Equal(condition, listing.Condition);
        Assert.True(listing.IsNewListing);
        Assert.NotEqual(default, listing.FirstSeenDate);
        Assert.NotEqual(default, listing.LastSeenDate);
        Assert.NotEqual(default, listing.CreatedAt);
    }

    [Fact]
    public void Create_ShouldRaiseListingCreatedEvent()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var make = "Honda";
        var model = "Civic";

        // Act
        var listing = Listing.Create(
            tenantId,
            "EXT-456",
            "TestSite",
            "https://example.com/listing/456",
            make,
            model,
            2021,
            20000m,
            Condition.New);

        // Assert
        Assert.Single(listing.DomainEvents);
        var domainEvent = listing.DomainEvents.First();
        Assert.IsType<AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Events.ListingCreated>(domainEvent);
    }

    [Fact]
    public void UpdatePrice_ShouldUpdatePriceAndRaiseEvent()
    {
        // Arrange
        var listing = Listing.Create(
            Guid.NewGuid(),
            "EXT-789",
            "TestSite",
            "https://example.com/listing/789",
            "Ford",
            "F-150",
            2019,
            30000m,
            Condition.Used);

        var newPrice = 28000m;

        // Act
        listing.UpdatePrice(newPrice);

        // Assert
        Assert.Equal(newPrice, listing.Price);
        Assert.NotNull(listing.UpdatedAt);
        Assert.Equal(2, listing.DomainEvents.Count);
    }

    [Fact]
    public void UpdatePrice_WithSamePrice_ShouldNotRaiseEvent()
    {
        // Arrange
        var price = 30000m;
        var listing = Listing.Create(
            Guid.NewGuid(),
            "EXT-999",
            "TestSite",
            "https://example.com/listing/999",
            "Chevrolet",
            "Silverado",
            2018,
            price,
            Condition.Certified);

        // Act
        listing.UpdatePrice(price);

        // Assert
        Assert.Equal(price, listing.Price);
        Assert.Single(listing.DomainEvents); // Only the created event
    }

    [Fact]
    public void MarkAsSeen_ShouldUpdateLastSeenDateAndClearNewFlag()
    {
        // Arrange
        var listing = Listing.Create(
            Guid.NewGuid(),
            "EXT-111",
            "TestSite",
            "https://example.com/listing/111",
            "Tesla",
            "Model 3",
            2022,
            45000m,
            Condition.New);

        var originalLastSeenDate = listing.LastSeenDate;
        Thread.Sleep(10); // Small delay to ensure time difference

        // Act
        listing.MarkAsSeen();

        // Assert
        Assert.False(listing.IsNewListing);
        Assert.True(listing.LastSeenDate > originalLastSeenDate);
    }

    [Fact]
    public void Create_WithOptionalFields_ShouldSetAllFields()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var imageUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" };

        // Act
        var listing = Listing.Create(
            tenantId,
            "EXT-222",
            "TestSite",
            "https://example.com/listing/222",
            "BMW",
            "X5",
            2021,
            55000m,
            Condition.Used,
            trim: "M Sport",
            mileage: 15000,
            vin: "1HGBH41JXMN109186",
            city: "Los Angeles",
            state: "CA",
            zipCode: "90001",
            transmission: Transmission.Automatic,
            fuelType: FuelType.Gasoline,
            bodyStyle: "SUV",
            exteriorColor: "Black",
            interiorColor: "Beige",
            description: "Excellent condition",
            imageUrls: imageUrls);

        // Assert
        Assert.Equal("M Sport", listing.Trim);
        Assert.Equal(15000, listing.Mileage);
        Assert.Equal("1HGBH41JXMN109186", listing.Vin);
        Assert.Equal("Los Angeles", listing.City);
        Assert.Equal("CA", listing.State);
        Assert.Equal("90001", listing.ZipCode);
        Assert.Equal(Transmission.Automatic, listing.Transmission);
        Assert.Equal(FuelType.Gasoline, listing.FuelType);
        Assert.Equal("SUV", listing.BodyStyle);
        Assert.Equal("Black", listing.ExteriorColor);
        Assert.Equal("Beige", listing.InteriorColor);
        Assert.Equal("Excellent condition", listing.Description);
        Assert.Equal(2, listing.ImageUrls.Count);
    }

    [Fact]
    public void ListingId_Create_ShouldGenerateUniqueIds()
    {
        // Act
        var id1 = ListingId.Create();
        var id2 = ListingId.Create();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
    }

    [Fact]
    public void ListingId_ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var listingId = ListingId.Create();

        // Act
        Guid guidValue = listingId;

        // Assert
        Assert.Equal(listingId.Value, guidValue);
    }

    [Fact]
    public void ListingId_ImplicitConversion_FromGuid_ShouldWork()
    {
        // Arrange
        var guidValue = Guid.NewGuid();

        // Act
        ListingId listingId = guidValue;

        // Assert
        Assert.Equal(guidValue, listingId.Value);
    }
}
