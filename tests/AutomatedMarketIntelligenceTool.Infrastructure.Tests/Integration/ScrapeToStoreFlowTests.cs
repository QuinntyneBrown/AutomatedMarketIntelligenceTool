using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Integration;

/// <summary>
/// E2E integration tests for the scrape-to-store workflow.
/// Tests the complete flow: scraping to duplicate detection to persistence.
/// </summary>
public class ScrapeToStoreFlowTests
{
    private readonly Guid _testTenantId = Guid.NewGuid();

    [Fact]
    public async Task ScrapeToStoreFlow_NewListing_ShouldPersistSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new AutomatedMarketIntelligenceToolContext(options);
        var duplicateDetectionService = new DuplicateDetectionService(
            context,
            NullLogger<DuplicateDetectionService>.Instance);

        // Simulate a scraped listing
        var scrapedListing = new ScrapedListing
        {
            ExternalId = "TEST123",
            SourceSite = "autotrader",
            ListingUrl = "https://autotrader.com/listing/TEST123",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 25000,
            Condition = Condition.Used,
            Mileage = 30000,
            Vin = "1HGBH41JXMN109186",
            City = "Toronto",
            Province = "ON",
            PostalCode = "M5V 3L9"
        };

        var scrapedListingInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = scrapedListing.ExternalId,
            SourceSite = scrapedListing.SourceSite,
            Vin = scrapedListing.Vin
        };

        // Act - Check for duplicate
        var duplicateResult = await duplicateDetectionService.CheckForDuplicateAsync(scrapedListingInfo);

        // Assert - Should be new listing
        Assert.False(duplicateResult.IsDuplicate);

        // Act - Create and persist listing
        var listing = Listing.Create(
            tenantId: _testTenantId,
            externalId: scrapedListing.ExternalId,
            sourceSite: scrapedListing.SourceSite,
            listingUrl: scrapedListing.ListingUrl,
            make: scrapedListing.Make,
            model: scrapedListing.Model,
            year: scrapedListing.Year,
            price: scrapedListing.Price,
            condition: scrapedListing.Condition,
            mileage: scrapedListing.Mileage,
            vin: scrapedListing.Vin,
            city: scrapedListing.City,
            province: scrapedListing.Province,
            postalCode: scrapedListing.PostalCode);

        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        // Assert - Verify persisted
        var savedListing = await context.Listings.FirstOrDefaultAsync(l => l.ExternalId == "TEST123");
        Assert.NotNull(savedListing);
        Assert.Equal("Toyota", savedListing.Make);
        Assert.Equal("Camry", savedListing.Model);
        Assert.Equal(2020, savedListing.Year);
        Assert.Equal(25000, savedListing.Price);
    }

    [Fact]
    public async Task ScrapeToStoreFlow_DuplicateVin_ShouldDetectAndUpdate()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new AutomatedMarketIntelligenceToolContext(options);
        var duplicateDetectionService = new DuplicateDetectionService(
            context,
            NullLogger<DuplicateDetectionService>.Instance);

        var vin = "1HGBH41JXMN109186";

        // Create existing listing
        var existingListing = Listing.Create(
            tenantId: _testTenantId,
            externalId: "ORIGINAL123",
            sourceSite: "cars.com",
            listingUrl: "https://cars.com/listing/ORIGINAL123",
            make: "Toyota",
            model: "Camry",
            year: 2020,
            price: 26000,
            condition: Condition.Used,
            vin: vin);

        context.Listings.Add(existingListing);
        await context.SaveChangesAsync();

        // Simulate new scrape with same VIN but different source
        var scrapedListingInfo = new ScrapedListingInfo
        {
            TenantId = _testTenantId,
            ExternalId = "NEWLISTING456",
            SourceSite = "autotrader",
            Vin = vin
        };

        // Act - Check for duplicate
        var duplicateResult = await duplicateDetectionService.CheckForDuplicateAsync(scrapedListingInfo);

        // Assert - Should detect VIN match
        Assert.True(duplicateResult.IsDuplicate);
        Assert.Equal(DuplicateMatchType.VinMatch, duplicateResult.MatchType);
        Assert.Equal(existingListing.ListingId.Value, duplicateResult.ExistingListingId);

        // Act - Update existing listing price
        var listingToUpdate = await context.Listings.FirstOrDefaultAsync(
            l => l.ListingId.Value == duplicateResult.ExistingListingId);

        Assert.NotNull(listingToUpdate);
        listingToUpdate.UpdatePrice(25500);
        listingToUpdate.MarkAsSeen();
        await context.SaveChangesAsync();

        // Assert - Verify update
        var updatedListing = await context.Listings.FirstOrDefaultAsync(
            l => l.ListingId.Value == existingListing.ListingId.Value);

        Assert.NotNull(updatedListing);
        Assert.Equal(25500, updatedListing.Price);
        Assert.NotNull(updatedListing.UpdatedAt);
    }
}
