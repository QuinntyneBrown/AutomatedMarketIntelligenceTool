using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ScrapedListingAggregate;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging;

Console.WriteLine("==============================================");
Console.WriteLine("  Volkswagen Search - TabangiMotors.com");
Console.WriteLine("==============================================");
Console.WriteLine();

// Set up logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug) // Changed to Debug for more detailed logging
        .AddConsole();
});

// Create the TabangiMotorsScraper
var logger = loggerFactory.CreateLogger<TabangiMotorsScraper>();
var scraper = new TabangiMotorsScraper(logger);

// Configure search parameters for Volkswagen
var searchParams = new SearchParameters
{
    Make = "Volkswagen",
    Model = "", // Search all Volkswagen models
    YearMin = 2000,
    YearMax = 2025,
    Province = CanadianProvince.ON,
    PostalCode = "M5V", // Toronto area
    MaxPages = 3 // Limit to 3 pages for playground testing
};

Console.WriteLine($"Searching for: {searchParams.YearMin}-{searchParams.YearMax} {searchParams.Make}");
Console.WriteLine($"Province: Ontario (ON)");
Console.WriteLine($"Max pages: {searchParams.MaxPages}");
Console.WriteLine();

// Create progress reporter
var progress = new Progress<ScrapeProgress>(p =>
{
    Console.WriteLine($"[{p.SiteName}] Page {p.CurrentPage} - Found {p.TotalListingsFound} listings so far");
});

Console.WriteLine($"--- Searching {scraper.SiteName} ---");
Console.WriteLine();

List<ScrapedListing> listings;

try
{
    listings = (await scraper.ScrapeAsync(searchParams, progress)).ToList();
    Console.WriteLine($"Found {listings.Count} listings on {scraper.SiteName}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error scraping {scraper.SiteName}: {ex.Message}");
    listings = new List<ScrapedListing>();
}

// Display results
Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine($"  RESULTS: Found {listings.Count} total listings");
Console.WriteLine("==============================================");
Console.WriteLine();

if (listings.Count == 0)
{
    Console.WriteLine("No listings found. Try adjusting search parameters or check if the site is accessible.");
}
else
{
    // Sort by price
    var sortedListings = listings
        .Where(x => x.Year >= searchParams.YearMin && x.Year <= searchParams.YearMax)
        .OrderBy(l => l.Price).ToList();

    foreach (var listing in sortedListings)
    {
        Console.WriteLine($"----------------------------------------------");
        Console.WriteLine($"  {listing.Year} {listing.Make} {listing.Model} {listing.Trim ?? ""}");
        Console.WriteLine($"  Price: ${listing.Price:N0} {listing.Currency}");
        Console.WriteLine($"  Mileage: {(listing.Mileage.HasValue ? $"{listing.Mileage.Value:N0} km" : "N/A")}");
        Console.WriteLine($"  Location: {listing.City}, {listing.Province}");
        Console.WriteLine($"  Condition: {listing.Condition}");
        if (listing.Transmission.HasValue)
            Console.WriteLine($"  Transmission: {listing.Transmission}");
        if (!string.IsNullOrEmpty(listing.ExteriorColor))
            Console.WriteLine($"  Color: {listing.ExteriorColor}");
        Console.WriteLine($"  Source: {listing.SourceSite}");
        Console.WriteLine($"  URL: {listing.ListingUrl}");
    }

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine("  SUMMARY");
    Console.WriteLine("==============================================");
    Console.WriteLine($"  Total listings: {listings.Count}");
    if (sortedListings.Any())
    {
        Console.WriteLine($"  Price range: ${sortedListings.Min(l => l.Price):N0} - ${sortedListings.Max(l => l.Price):N0}");
        Console.WriteLine($"  Average price: ${sortedListings.Average(l => l.Price):N0}");
    }
}

Console.WriteLine();
Console.WriteLine("Search complete!");
