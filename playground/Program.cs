using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging;

Console.WriteLine("==============================================");
Console.WriteLine("  2025 Jetta GLI Search - Ontario, Canada");
Console.WriteLine("==============================================");
Console.WriteLine();

// Set up logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole();
});

// Create the scraper factory
var scraperFactory = new ScraperFactory(loggerFactory);

// Configure search parameters for 2025 Jetta GLI in Ontario
var searchParams = new SearchParameters
{
    Make = "Volkswagen",
    Model = "Jetta",
    Province = CanadianProvince.ON,
    MaxPages = 3 // Limit to 3 pages for playground testing
};

Console.WriteLine($"Searching for: {searchParams.YearMin} {searchParams.Make} {searchParams.Model}");
Console.WriteLine($"Province: Ontario (ON)");
Console.WriteLine($"Max pages: {searchParams.MaxPages}");
Console.WriteLine();

// Create progress reporter
var progress = new Progress<ScrapeProgress>(p =>
{
    Console.WriteLine($"[{p.SiteName}] Page {p.CurrentPage} - Found {p.TotalListingsFound} listings so far");
});

// Get all scrapers and search
var scrapers = scraperFactory.CreateAllScrapers();
var allListings = new List<ScrapedListing>();

foreach (var scraper in scrapers)
{
    Console.WriteLine($"\n--- Searching {scraper.SiteName} ---");

    try
    {
        var listings = await scraper.ScrapeAsync(searchParams, progress);
        var listingsList = listings.ToList();
        allListings.AddRange(listingsList);

        Console.WriteLine($"Found {listingsList.Count} listings on {scraper.SiteName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error scraping {scraper.SiteName}: {ex.Message}");
    }
}

// Display results
Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine($"  RESULTS: Found {allListings.Count} total listings");
Console.WriteLine("==============================================");
Console.WriteLine();

if (allListings.Count == 0)
{
    Console.WriteLine("No listings found. Try adjusting search parameters or check if the sites are accessible.");
}
else
{
    // Sort by price
    var sortedListings = allListings.OrderBy(l => l.Price).ToList();

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
    Console.WriteLine($"  Total listings: {allListings.Count}");
    Console.WriteLine($"  Price range: ${sortedListings.Min(l => l.Price):N0} - ${sortedListings.Max(l => l.Price):N0}");
    Console.WriteLine($"  Average price: ${sortedListings.Average(l => l.Price):N0}");
}

Console.WriteLine();
Console.WriteLine("Search complete!");
