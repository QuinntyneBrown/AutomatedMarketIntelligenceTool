using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Cli.Formatters;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to search for car listings in the database.
/// </summary>
public class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    private readonly ISearchService _searchService;
    private readonly Dictionary<string, IOutputFormatter> _formatters;

    public SearchCommand(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        
        _formatters = new Dictionary<string, IOutputFormatter>(StringComparer.OrdinalIgnoreCase)
        {
            ["table"] = new TableFormatter(),
            ["json"] = new JsonFormatter(),
            ["csv"] = new CsvFormatter()
        };
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Validate format
            if (!_formatters.ContainsKey(settings.Format))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid format '{settings.Format}'. Valid formats are: table, json, csv[/]");
                return ExitCodes.ValidationError;
            }

            // Build search criteria
            var criteria = new SearchCriteria
            {
                TenantId = settings.TenantId,
                Makes = settings.Makes,
                Models = settings.Models,
                YearMin = settings.YearMin,
                YearMax = settings.YearMax,
                PriceMin = settings.PriceMin,
                PriceMax = settings.PriceMax,
                MileageMin = settings.MileageMin,
                MileageMax = settings.MileageMax,
                PostalCode = settings.ZipCode,
                RadiusKilometers = settings.Radius,
                Page = settings.Page,
                PageSize = settings.PageSize
            };

            // Execute search with progress indicator
            SearchResult? result = null;
            
            await AnsiConsole.Status()
                .StartAsync("Searching listings...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    result = await _searchService.SearchListingsAsync(criteria);
                });

            if (result == null)
            {
                AnsiConsole.MarkupLine("[red]Error: Search failed[/]");
                return ExitCodes.GeneralError;
            }

            // Format and display results
            var formatter = _formatters[settings.Format];
            
            // Create simplified output objects for display
            var displayResults = result.Listings.Select(lr => new
            {
                Make = lr.Listing.Make,
                Model = lr.Listing.Model,
                Year = lr.Listing.Year,
                Price = lr.Listing.Price,
                Mileage = lr.Listing.Mileage,
                Location = $"{lr.Listing.City}, {lr.Listing.Province}",
                Distance = lr.DistanceKilometers.HasValue ? $"{lr.DistanceKilometers:F1} km" : "N/A",
                Url = lr.Listing.ListingUrl
            });

            formatter.Format(displayResults);

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("-m|--make")]
        [Description("Vehicle make (e.g., Toyota, Honda)")]
        public string[]? Makes { get; set; }

        [CommandOption("--model")]
        [Description("Vehicle model (e.g., Camry, Civic)")]
        public string[]? Models { get; set; }

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

        [CommandOption("--mileage-min")]
        [Description("Minimum mileage")]
        public int? MileageMin { get; set; }

        [CommandOption("--mileage-max")]
        [Description("Maximum mileage")]
        public int? MileageMax { get; set; }

        [CommandOption("-z|--zip")]
        [Description("ZIP code for location search")]
        public string? ZipCode { get; set; }

        [CommandOption("-r|--radius")]
        [Description("Search radius in miles (default: 25)")]
        [DefaultValue(25)]
        public double Radius { get; set; } = 25;

        [CommandOption("-f|--format")]
        [Description("Output format: table, json, csv (default: table)")]
        [DefaultValue("table")]
        public string Format { get; set; } = "table";

        [CommandOption("-p|--page")]
        [Description("Page number (default: 1)")]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        [CommandOption("--page-size")]
        [Description("Results per page (default: 30)")]
        [DefaultValue(30)]
        public int PageSize { get; set; } = 30;
    }
}
