using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingWorker.Core.Models;
using ScrapingWorker.Core.Services;
using ScrapingWorker.Infrastructure.Extensions;

Console.WriteLine("=== Automated Market Intelligence Tool - Playground ===");
Console.WriteLine("Kijiji Autos scraping test for: 2007 Toyota Camry");
Console.WriteLine();

// Set up dependency injection
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add scraping infrastructure
services.AddScrapingWorkerInfrastructure();

var serviceProvider = services.BuildServiceProvider();

// Get the scraper
var scraperFactory = serviceProvider.GetRequiredService<IScraperFactory>();

if (!scraperFactory.HasScraper(ScrapingOrchestration.Core.Enums.ScrapingSource.Kijiji))
{
    Console.WriteLine("Error: Kijiji scraper not available");
    return 1;
}

var scraper = scraperFactory.GetScraper(ScrapingOrchestration.Core.Enums.ScrapingSource.Kijiji);

// Define search parameters for 2007 Toyota Camry
var parameters = new SearchParameters
{
    Make = "Toyota",
    Model = "Camry",
    YearFrom = 2007,
    YearTo = 2007,
    MaxResults = 50
};

Console.WriteLine("Search Parameters:");
Console.WriteLine($"  Make: {parameters.Make}");
Console.WriteLine($"  Model: {parameters.Model}");
Console.WriteLine($"  Year: {parameters.YearFrom}-{parameters.YearTo}");
Console.WriteLine($"  Max Results: {parameters.MaxResults}");
Console.WriteLine();

Console.WriteLine("Starting scrape...");
Console.WriteLine();

var progress = new Progress<ScrapeProgress>(p =>
{
    Console.WriteLine($"  Progress: Page {p.CurrentPage}/{p.TotalPages}, Listings found: {p.ListingsScraped}");
});

try
{
    var result = await scraper.ScrapeAsync(parameters, progress, CancellationToken.None);

    Console.WriteLine();
    Console.WriteLine("=== Scrape Results ===");
    Console.WriteLine($"Success: {result.Success}");
    Console.WriteLine($"Total Listings Found: {result.TotalListingsFound}");
    Console.WriteLine($"Pages Crawled: {result.PagesCrawled}");
    Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");

    if (!string.IsNullOrEmpty(result.ErrorMessage))
    {
        Console.WriteLine($"Error: {result.ErrorMessage}");
    }

    if (result.Listings.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("=== Sample Listings ===");

        foreach (var listing in result.Listings.Take(10))
        {
            Console.WriteLine();
            Console.WriteLine($"  Title: {listing.Title}");
            Console.WriteLine($"  Price: {(listing.Price.HasValue ? $"${listing.Price:N0}" : "N/A")}");
            Console.WriteLine($"  Year: {listing.Year?.ToString() ?? "N/A"}");
            Console.WriteLine($"  Mileage: {(listing.Mileage.HasValue ? $"{listing.Mileage:N0} km" : "N/A")}");
            Console.WriteLine($"  URL: {listing.ListingUrl}");
        }

        if (result.Listings.Count > 10)
        {
            Console.WriteLine();
            Console.WriteLine($"  ... and {result.Listings.Count - 10} more listings");
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== Playground Complete ===");
    return 0;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"HTTP Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("The scraper may have been blocked or the site structure may have changed.");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}
