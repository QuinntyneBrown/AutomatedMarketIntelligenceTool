using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to scrape car listings from automotive websites.
/// </summary>
public class ScrapeCommand : AsyncCommand<ScrapeCommand.Settings>
{
    private readonly IScraperFactory _scraperFactory;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<ScrapeCommand> _logger;

    public ScrapeCommand(
        IScraperFactory scraperFactory,
        IDuplicateDetectionService duplicateDetectionService,
        IAutomatedMarketIntelligenceToolContext context,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<ScrapeCommand> logger)
    {
        _scraperFactory = scraperFactory ?? throw new ArgumentNullException(nameof(scraperFactory));
        _duplicateDetectionService = duplicateDetectionService ?? throw new ArgumentNullException(nameof(duplicateDetectionService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var correlationId = Guid.NewGuid().ToString();
        _correlationIdProvider.SetCorrelationId(correlationId);
        
        try
        {
            _logger.LogInformation(
                "Starting scrape operation with CorrelationId: {CorrelationId}, TenantId: {TenantId}, Sites: {Site}",
                correlationId,
                settings.TenantId,
                settings.Site);

            // Validate site
            var supportedSites = _scraperFactory.GetSupportedSites().ToList();
            
            if (!string.IsNullOrEmpty(settings.Site) && 
                settings.Site.ToLowerInvariant() != "all" &&
                !supportedSites.Any(s => s.Equals(settings.Site, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "Invalid site specified: {Site}. Valid sites: {ValidSites}",
                    settings.Site,
                    string.Join(", ", supportedSites));
                
                AnsiConsole.MarkupLine($"[red]Error: Invalid site '{settings.Site}'. Valid sites are: {string.Join(", ", supportedSites)}, or 'all'[/]");
                return ExitCodes.ValidationError;
            }

            // Build search parameters
            var searchParams = new SearchParameters
            {
                Make = settings.Make,
                Model = settings.Model,
                YearMin = settings.YearMin,
                YearMax = settings.YearMax,
                PriceMin = settings.PriceMin,
                PriceMax = settings.PriceMax,
                MileageMax = settings.MileageMax,
                ZipCode = settings.ZipCode,
                RadiusMiles = settings.Radius,
                MaxPages = settings.MaxPages
            };

            // Determine which scrapers to use
            var scrapers = string.IsNullOrEmpty(settings.Site) || settings.Site.ToLowerInvariant() == "all"
                ? _scraperFactory.CreateAllScrapers().ToList()
                : new List<ISiteScraper> { _scraperFactory.CreateScraper(settings.Site) };

            _logger.LogInformation("Executing scraping with {ScraperCount} scraper(s)", scrapers.Count);
            AnsiConsole.MarkupLine($"[green]Starting scrape with {scrapers.Count} scraper(s)...[/]");

            int totalScraped = 0;
            int totalSaved = 0;
            int totalDuplicates = 0;

            // Execute scraping for each site
            foreach (var scraper in scrapers)
            {
                await AnsiConsole.Progress()
                    .AutoClear(false)
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn())
                    .StartAsync(async ctx =>
                    {
                        var progressTask = ctx.AddTask($"[green]Scraping {scraper.SiteName}[/]");

                        var progress = new Progress<ScrapeProgress>(p =>
                        {
                            progressTask.Description = $"[green]Scraping {scraper.SiteName}[/] - Page {p.CurrentPage} - {p.TotalListingsFound} listings found";
                            progressTask.Increment(1);
                        });

                        try
                        {
                            _logger.LogInformation("Starting scrape for site: {SiteName}", scraper.SiteName);
                            
                            var listings = await scraper.ScrapeAsync(searchParams, progress);
                            var listingsList = listings.ToList();
                            totalScraped += listingsList.Count;

                            _logger.LogInformation(
                                "Scraped {Count} listings from {SiteName}",
                                listingsList.Count,
                                scraper.SiteName);

                            progressTask.Description = $"[green]Processing {listingsList.Count} listings from {scraper.SiteName}[/]";

                            // Process each listing
                            foreach (var scrapedListing in listingsList)
                            {
                                // Convert to ScrapedListingInfo for duplicate detection
                                var scrapedListingInfo = new ScrapedListingInfo
                                {
                                    TenantId = settings.TenantId,
                                    ExternalId = scrapedListing.ExternalId,
                                    SourceSite = scrapedListing.SourceSite,
                                    Vin = scrapedListing.Vin
                                };

                                var duplicateResult = await _duplicateDetectionService.CheckForDuplicateAsync(scrapedListingInfo);

                                if (duplicateResult.IsDuplicate && duplicateResult.ExistingListingId.HasValue)
                                {
                                    totalDuplicates++;
                                    
                                    // Load the existing listing to update price if changed
                                    var existingListing = await _context.Listings
                                        .FirstOrDefaultAsync(l => l.ListingId.Value == duplicateResult.ExistingListingId.Value);
                                    
                                    if (existingListing != null && scrapedListing.Price != existingListing.Price)
                                    {
                                        existingListing.UpdatePrice(scrapedListing.Price);
                                        existingListing.MarkAsSeen();
                                    }
                                }
                                else
                                {
                                    var listing = Core.Models.ListingAggregate.Listing.Create(
                                        tenantId: settings.TenantId,
                                        externalId: scrapedListing.ExternalId,
                                        sourceSite: scrapedListing.SourceSite,
                                        listingUrl: scrapedListing.ListingUrl,
                                        make: scrapedListing.Make,
                                        model: scrapedListing.Model,
                                        year: scrapedListing.Year,
                                        price: scrapedListing.Price,
                                        condition: scrapedListing.Condition,
                                        trim: scrapedListing.Trim,
                                        mileage: scrapedListing.Mileage,
                                        vin: scrapedListing.Vin,
                                        city: scrapedListing.City,
                                        state: scrapedListing.State,
                                        zipCode: scrapedListing.ZipCode,
                                        transmission: scrapedListing.Transmission,
                                        fuelType: scrapedListing.FuelType,
                                        bodyStyle: scrapedListing.BodyStyle,
                                        exteriorColor: scrapedListing.ExteriorColor,
                                        interiorColor: scrapedListing.InteriorColor,
                                        description: scrapedListing.Description,
                                        imageUrls: scrapedListing.ImageUrls);

                                    _context.Listings.Add(listing);
                                    totalSaved++;
                                }
                            }

                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation(
                                "Saved {NewCount} new listings and updated {DuplicateCount} duplicates from {SiteName}",
                                totalSaved,
                                totalDuplicates,
                                scraper.SiteName);
                            
                            progressTask.StopTask();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error scraping {SiteName}: {ErrorMessage}",
                                scraper.SiteName,
                                ex.Message);
                            
                            progressTask.StopTask();
                            AnsiConsole.MarkupLine($"[red]Error scraping {scraper.SiteName}: {ex.Message}[/]");
                        }
                    });
            }

            // Display summary
            var summaryTable = new Table();
            summaryTable.Border(TableBorder.Rounded);
            summaryTable.AddColumn("[bold]Metric[/]");
            summaryTable.AddColumn("[bold]Count[/]");
            summaryTable.AddRow("Total Scraped", totalScraped.ToString());
            summaryTable.AddRow("[green]New Listings Saved[/]", $"[green]{totalSaved}[/]");
            summaryTable.AddRow("[yellow]Duplicates Found[/]", $"[yellow]{totalDuplicates}[/]");

            AnsiConsole.Write(summaryTable);

            _logger.LogInformation(
                "Scrape operation completed. Total: {TotalScraped}, New: {TotalSaved}, Duplicates: {TotalDuplicates}",
                totalScraped,
                totalSaved,
                totalDuplicates);

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape operation failed: {ErrorMessage}", ex.Message);
            
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.ScrapingError;
        }
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("-s|--site")]
        [Description("Site to scrape (autotrader, cars.com, or 'all' for all sites)")]
        [DefaultValue("all")]
        public string Site { get; set; } = "all";

        [CommandOption("-m|--make")]
        [Description("Vehicle make to search for")]
        public string? Make { get; set; }

        [CommandOption("--model")]
        [Description("Vehicle model to search for")]
        public string? Model { get; set; }

        [CommandOption("--year-min")]
        [Description("Minimum year")]
        public int? YearMin { get; set; }

        [CommandOption("--year-max")]
        [Description("Maximum year")]
        public int? YearMax { get; set; }

        [CommandOption("--price-min")]
        [Description("Minimum price")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max")]
        [Description("Maximum price")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--mileage-max")]
        [Description("Maximum mileage")]
        public int? MileageMax { get; set; }

        [CommandOption("-z|--zip")]
        [Description("ZIP code for location search")]
        public string? ZipCode { get; set; }

        [CommandOption("-r|--radius")]
        [Description("Search radius in miles")]
        public int? Radius { get; set; }

        [CommandOption("--max-pages")]
        [Description("Maximum number of pages to scrape (default: 50)")]
        [DefaultValue(50)]
        public int MaxPages { get; set; } = 50;
    }
}
