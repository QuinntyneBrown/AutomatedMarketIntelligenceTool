using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;
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
    private readonly IConcurrentScrapingEngine? _concurrentScrapingEngine;
    private readonly ILogger<ScrapeCommand> _logger;

    public ScrapeCommand(
        IScraperFactory scraperFactory,
        IDuplicateDetectionService duplicateDetectionService,
        IAutomatedMarketIntelligenceToolContext context,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<ScrapeCommand> logger,
        IConcurrentScrapingEngine? concurrentScrapingEngine = null)
    {
        _scraperFactory = scraperFactory ?? throw new ArgumentNullException(nameof(scraperFactory));
        _duplicateDetectionService = duplicateDetectionService ?? throw new ArgumentNullException(nameof(duplicateDetectionService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
        _concurrentScrapingEngine = concurrentScrapingEngine;
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

            // Validate site options
            var supportedSites = _scraperFactory.GetSupportedSites().ToList();
            
            // Validate individual site option
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

            // Validate --sites option
            if (!string.IsNullOrEmpty(settings.Sites))
            {
                var requestedSites = settings.Sites.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var invalidSites = requestedSites.Where(s => !supportedSites.Any(ss => ss.Equals(s, StringComparison.OrdinalIgnoreCase))).ToList();
                
                if (invalidSites.Any())
                {
                    _logger.LogWarning(
                        "Invalid sites specified: {InvalidSites}. Valid sites: {ValidSites}",
                        string.Join(", ", invalidSites),
                        string.Join(", ", supportedSites));
                    
                    AnsiConsole.MarkupLine($"[red]Error: Invalid sites '{string.Join(", ", invalidSites)}'. Valid sites are: {string.Join(", ", supportedSites)}[/]");
                    return ExitCodes.ValidationError;
                }
            }

            // Validate --exclude-sites option
            if (!string.IsNullOrEmpty(settings.ExcludeSites))
            {
                var excludedSites = settings.ExcludeSites.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var invalidSites = excludedSites.Where(s => !supportedSites.Any(ss => ss.Equals(s, StringComparison.OrdinalIgnoreCase))).ToList();
                
                if (invalidSites.Any())
                {
                    _logger.LogWarning(
                        "Invalid exclude sites specified: {InvalidSites}. Valid sites: {ValidSites}",
                        string.Join(", ", invalidSites),
                        string.Join(", ", supportedSites));
                    
                    AnsiConsole.MarkupLine($"[red]Error: Invalid exclude sites '{string.Join(", ", invalidSites)}'. Valid sites are: {string.Join(", ", supportedSites)}[/]");
                    return ExitCodes.ValidationError;
                }
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
                PostalCode = settings.ZipCode,
                RadiusKilometers = settings.Radius,
                MaxPages = settings.MaxPages,
                HeadedMode = settings.HeadedMode,
                ScreenshotOnError = settings.ScreenshotOnError,
                ScreenshotAll = settings.ScreenshotAll,
                SaveHtml = settings.SaveHtml
            };

            // Determine which scrapers to use
            List<ISiteScraper> scrapers;
            
            if (!string.IsNullOrEmpty(settings.Sites))
            {
                // Use --sites option (comma-separated list)
                var requestedSites = settings.Sites.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                scrapers = requestedSites.Select(site => _scraperFactory.CreateScraper(site)).ToList();
            }
            else if (!string.IsNullOrEmpty(settings.Site) && settings.Site.ToLowerInvariant() != "all")
            {
                // Use single -s/--site option
                scrapers = new List<ISiteScraper> { _scraperFactory.CreateScraper(settings.Site) };
            }
            else
            {
                // Use all scrapers by default
                scrapers = _scraperFactory.CreateAllScrapers().ToList();
            }
            
            // Apply exclusions if specified
            if (!string.IsNullOrEmpty(settings.ExcludeSites))
            {
                var excludedSites = settings.ExcludeSites.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var excludeSet = new HashSet<string>(excludedSites, StringComparer.OrdinalIgnoreCase);
                scrapers = scrapers.Where(s => !excludeSet.Contains(s.SiteName)).ToList();
            }

            _logger.LogInformation("Executing scraping with {ScraperCount} scraper(s)", scrapers.Count);
            AnsiConsole.MarkupLine($"[green]Starting scrape with {scrapers.Count} scraper(s)...[/]");

            ScrapeResults results;

            // Use concurrent scraping if enabled and available
            if (settings.Concurrency > 1 && _concurrentScrapingEngine != null && scrapers.Count > 1)
            {
                _logger.LogInformation(
                    "Using concurrent scraping with concurrency level: {Concurrency}",
                    settings.Concurrency);

                results = await ExecuteConcurrentScrapingAsync(
                    scrapers,
                    searchParams,
                    settings);
            }
            else
            {
                // Execute scraping sequentially for each site
                results = await ExecuteSequentialScrapingAsync(
                    scrapers,
                    searchParams,
                    settings);
            }

            // Display summary
            var summaryTable = new Table();
            summaryTable.Border(TableBorder.Rounded);
            summaryTable.AddColumn("[bold]Metric[/]");
            summaryTable.AddColumn("[bold]Count[/]");
            summaryTable.AddRow("Total Scraped", results.TotalScraped.ToString());
            summaryTable.AddRow("[green]New Listings Saved[/]", $"[green]{results.TotalSaved}[/]");
            summaryTable.AddRow("[yellow]Duplicates Found[/]", $"[yellow]{results.TotalDuplicates}[/]");

            AnsiConsole.Write(summaryTable);

            _logger.LogInformation(
                "Scrape operation completed. Total: {TotalScraped}, New: {TotalSaved}, Duplicates: {TotalDuplicates}",
                results.TotalScraped,
                results.TotalSaved,
                results.TotalDuplicates);

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

    private async Task<ScrapeResults> ExecuteSequentialScrapingAsync(
        List<ISiteScraper> scrapers,
        SearchParameters searchParams,
        Settings settings)
    {
        var totalScraped = 0;
        var totalSaved = 0;
        var totalDuplicates = 0;

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
                                    // Create new listing with all available fields
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
                                        province: scrapedListing.Province,
                                        postalCode: scrapedListing.PostalCode,
                                        currency: scrapedListing.Currency,
                                        transmission: scrapedListing.Transmission,
                                        fuelType: scrapedListing.FuelType,
                                        bodyStyle: scrapedListing.BodyStyle,
                                        drivetrain: scrapedListing.Drivetrain,
                                        exteriorColor: scrapedListing.ExteriorColor,
                                        interiorColor: scrapedListing.InteriorColor,
                                        sellerType: scrapedListing.SellerType,
                                        sellerName: scrapedListing.SellerName,
                                        sellerPhone: scrapedListing.SellerPhone,
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

        return new ScrapeResults
        {
            TotalScraped = totalScraped,
            TotalSaved = totalSaved,
            TotalDuplicates = totalDuplicates
        };
    }

    private async Task<ScrapeResults> ExecuteConcurrentScrapingAsync(
        List<ISiteScraper> scrapers,
        SearchParameters searchParams,
        Settings settings)
    {
        var totalScraped = 0;
        var totalSaved = 0;
        var totalDuplicates = 0;

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var progressTasks = new Dictionary<string, ProgressTask>();
                
                var progress = new Progress<ConcurrentScrapeProgress>(p =>
                {
                    if (!progressTasks.ContainsKey(p.SiteName))
                    {
                        progressTasks[p.SiteName] = ctx.AddTask($"[green]{p.SiteName}[/]");
                    }

                    var task = progressTasks[p.SiteName];
                    
                    switch (p.EventType)
                    {
                        case ConcurrentScrapeEventType.Started:
                            task.Description = $"[yellow]{p.SiteName}[/] - Starting...";
                            break;
                        case ConcurrentScrapeEventType.Completed:
                            task.Description = $"[green]{p.SiteName}[/] - {p.Message}";
                            task.StopTask();
                            break;
                        case ConcurrentScrapeEventType.Failed:
                            task.Description = $"[red]{p.SiteName}[/] - Failed";
                            task.StopTask();
                            break;
                    }
                });

                var results = await _concurrentScrapingEngine!.ScrapeAsync(
                    scrapers,
                    searchParams,
                    settings.Concurrency,
                    progress);

                // Process results
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        var listingsList = result.Listings.ToList();
                        totalScraped += listingsList.Count;

                        _logger.LogInformation(
                            "Processing {Count} listings from {SiteName}",
                            listingsList.Count,
                            result.SiteName);

                        foreach (var scrapedListing in listingsList)
                        {
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
                                    province: scrapedListing.Province,
                                    postalCode: scrapedListing.PostalCode,
                                    currency: scrapedListing.Currency,
                                    transmission: scrapedListing.Transmission,
                                    fuelType: scrapedListing.FuelType,
                                    bodyStyle: scrapedListing.BodyStyle,
                                    drivetrain: scrapedListing.Drivetrain,
                                    exteriorColor: scrapedListing.ExteriorColor,
                                    interiorColor: scrapedListing.InteriorColor,
                                    sellerType: scrapedListing.SellerType,
                                    sellerName: scrapedListing.SellerName,
                                    sellerPhone: scrapedListing.SellerPhone,
                                    description: scrapedListing.Description,
                                    imageUrls: scrapedListing.ImageUrls);

                                _context.Listings.Add(listing);
                                totalSaved++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogError(
                            "Scraping failed for {SiteName}: {ErrorMessage}",
                            result.SiteName,
                            result.ErrorMessage);
                    }
                }
            });

        return new ScrapeResults
        {
            TotalScraped = totalScraped,
            TotalSaved = totalSaved,
            TotalDuplicates = totalDuplicates
        };
    }

    private class ScrapeResults
    {
        public int TotalScraped { get; init; }
        public int TotalSaved { get; init; }
        public int TotalDuplicates { get; init; }
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("-s|--site")]
        [Description("Site to scrape (autotrader.ca, kijiji, or 'all' for all sites)")]
        [DefaultValue("all")]
        public string Site { get; set; } = "all";

        [CommandOption("--sites")]
        [Description("Comma-separated list of sites to scrape (e.g., 'autotrader,kijiji')")]
        public string? Sites { get; set; }

        [CommandOption("--exclude-sites")]
        [Description("Comma-separated list of sites to exclude (e.g., 'kijiji')")]
        public string? ExcludeSites { get; set; }

        [CommandOption("--headed")]
        [Description("Run browser in headed mode for debugging")]
        [DefaultValue(false)]
        public bool HeadedMode { get; set; }

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
        [Description("Minimum price in CAD")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max")]
        [Description("Maximum price in CAD")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--mileage-max")]
        [Description("Maximum mileage in kilometers")]
        public int? MileageMax { get; set; }

        [CommandOption("-p|--postal-code")]
        [Description("Canadian postal code for location search (e.g., M5V 3L9)")]
        public string? ZipCode { get; set; }

        [CommandOption("-r|--radius")]
        [Description("Search radius in kilometers")]
        public int? Radius { get; set; }

        [CommandOption("--max-pages")]
        [Description("Maximum number of pages to scrape (default: 50)")]
        [DefaultValue(50)]
        public int MaxPages { get; set; } = 50;

        [CommandOption("--concurrency")]
        [Description("Number of sites to scrape concurrently (default: 3, min: 1, max: 10)")]
        [DefaultValue(3)]
        public int Concurrency { get; set; } = 3;

        [CommandOption("--screenshot-on-error")]
        [Description("Capture screenshots when scraping errors occur")]
        [DefaultValue(false)]
        public bool ScreenshotOnError { get; set; }

        [CommandOption("--screenshot-all")]
        [Description("Capture screenshots of all pages during scraping")]
        [DefaultValue(false)]
        public bool ScreenshotAll { get; set; }

        [CommandOption("--save-html")]
        [Description("Save HTML source of pages for debugging")]
        [DefaultValue(false)]
        public bool SaveHtml { get; set; }
    }
}
