using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class ScrapedListingTests
{
    [Fact]
    public void ScrapedListing_CanBeCreatedWithRequiredProperties()
    {
        // Act
        var listing = new ScrapedListing
        {
            ExternalId = "EXT-001",
            SourceSite = "TestSite",
            ListingUrl = "https://example.com/listing/001",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 25000m,
            Condition = Condition.Used
        };

        // Assert
        Assert.Equal("EXT-001", listing.ExternalId);
        Assert.Equal("TestSite", listing.SourceSite);
        Assert.Equal("https://example.com/listing/001", listing.ListingUrl);
        Assert.Equal("Toyota", listing.Make);
        Assert.Equal("Camry", listing.Model);
        Assert.Equal(2020, listing.Year);
        Assert.Equal(25000m, listing.Price);
        Assert.Equal(Condition.Used, listing.Condition);
    }

    [Fact]
    public void ScrapedListing_OptionalPropertiesDefaultToNull()
    {
        // Act
        var listing = new ScrapedListing
        {
            ExternalId = "EXT-002",
            SourceSite = "TestSite",
            ListingUrl = "https://example.com/listing/002",
            Make = "Honda",
            Model = "Accord",
            Year = 2019,
            Price = 22000m,
            Condition = Condition.Used
        };

        // Assert
        Assert.Null(listing.Trim);
        Assert.Null(listing.Mileage);
        Assert.Null(listing.Vin);
        Assert.Null(listing.City);
        Assert.Null(listing.Province);
        Assert.Null(listing.PostalCode);
        Assert.Null(listing.Transmission);
        Assert.Null(listing.FuelType);
        Assert.Null(listing.BodyStyle);
        Assert.Null(listing.ExteriorColor);
        Assert.Null(listing.InteriorColor);
        Assert.Null(listing.Description);
    }

    [Fact]
    public void ScrapedListing_ImageUrlsDefaultsToEmptyList()
    {
        // Act
        var listing = new ScrapedListing
        {
            ExternalId = "EXT-003",
            SourceSite = "TestSite",
            ListingUrl = "https://example.com/listing/003",
            Make = "Ford",
            Model = "Mustang",
            Year = 2021,
            Price = 35000m,
            Condition = Condition.New
        };

        // Assert
        Assert.NotNull(listing.ImageUrls);
        Assert.Empty(listing.ImageUrls);
    }

    [Fact]
    public void ScrapedListing_CanSetAllProperties()
    {
        // Act
        var listing = new ScrapedListing
        {
            ExternalId = "EXT-004",
            SourceSite = "Autotrader.ca",
            ListingUrl = "https://autotrader.ca/listing/004",
            Make = "BMW",
            Model = "X5",
            Year = 2022,
            Trim = "xDrive40i",
            Price = 55000m,
            Mileage = 12000,
            Vin = "5UXCR6C08N9L12345",
            City = "Toronto",
            Province = "ON",
            PostalCode = "M5V 3L9",
            Currency = "CAD",
            Country = "CA",
            Condition = Condition.Certified,
            Transmission = Transmission.Automatic,
            FuelType = FuelType.Gasoline,
            BodyStyle = BodyStyle.SUV,
            ExteriorColor = "Black",
            InteriorColor = "Tan",
            Description = "Excellent condition, low miles",
            ImageUrls = new List<string>
            {
                "https://example.com/image1.jpg",
                "https://example.com/image2.jpg"
            }
        };

        // Assert
        Assert.Equal("EXT-004", listing.ExternalId);
        Assert.Equal("Autotrader.ca", listing.SourceSite);
        Assert.Equal("https://autotrader.ca/listing/004", listing.ListingUrl);
        Assert.Equal("BMW", listing.Make);
        Assert.Equal("X5", listing.Model);
        Assert.Equal(2022, listing.Year);
        Assert.Equal("xDrive40i", listing.Trim);
        Assert.Equal(55000m, listing.Price);
        Assert.Equal(12000, listing.Mileage);
        Assert.Equal("5UXCR6C08N9L12345", listing.Vin);
        Assert.Equal("Toronto", listing.City);
        Assert.Equal("ON", listing.Province);
        Assert.Equal("M5V 3L9", listing.PostalCode);
        Assert.Equal("CAD", listing.Currency);
        Assert.Equal("CA", listing.Country);
        Assert.Equal(Condition.Certified, listing.Condition);
        Assert.Equal(Transmission.Automatic, listing.Transmission);
        Assert.Equal(FuelType.Gasoline, listing.FuelType);
        Assert.Equal(BodyStyle.SUV, listing.BodyStyle);
        Assert.Equal("Black", listing.ExteriorColor);
        Assert.Equal("Tan", listing.InteriorColor);
        Assert.Equal("Excellent condition, low miles", listing.Description);
        Assert.Equal(2, listing.ImageUrls.Count);
    }

    [Fact]
    public void ScrapedListing_SupportsAllConditionEnums()
    {
        // Arrange & Act & Assert
        var newListing = new ScrapedListing
        {
            ExternalId = "1",
            SourceSite = "Test",
            ListingUrl = "http://test.com",
            Make = "Test",
            Model = "Test",
            Year = 2020,
            Price = 10000,
            Condition = Condition.New
        };
        Assert.Equal(Condition.New, newListing.Condition);

        var usedListing = new ScrapedListing
        {
            ExternalId = "2",
            SourceSite = "Test",
            ListingUrl = "http://test.com",
            Make = "Test",
            Model = "Test",
            Year = 2020,
            Price = 10000,
            Condition = Condition.Used
        };
        Assert.Equal(Condition.Used, usedListing.Condition);

        var certifiedListing = new ScrapedListing
        {
            ExternalId = "3",
            SourceSite = "Test",
            ListingUrl = "http://test.com",
            Make = "Test",
            Model = "Test",
            Year = 2020,
            Price = 10000,
            Condition = Condition.Certified
        };
        Assert.Equal(Condition.Certified, certifiedListing.Condition);
    }
}
