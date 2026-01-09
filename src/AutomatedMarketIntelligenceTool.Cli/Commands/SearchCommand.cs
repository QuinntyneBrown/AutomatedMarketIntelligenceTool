using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Cli.Formatters;
using AutomatedMarketIntelligenceTool.Cli.Interactive;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to search for car listings in the database.
/// </summary>
public class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    private readonly ISearchService _searchService;
    private readonly IAutoCompleteService? _autoCompleteService;
    private readonly ILogger<SearchCommand>? _logger;
    private readonly Dictionary<string, IOutputFormatter> _formatters;

    public SearchCommand(
        ISearchService searchService,
        IAutoCompleteService? autoCompleteService = null,
        ILogger<SearchCommand>? logger = null)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _autoCompleteService = autoCompleteService;
        _logger = logger;

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
            // Handle interactive mode
            if (settings.Interactive)
            {
                if (_autoCompleteService == null)
                {
                    AnsiConsole.MarkupLine("[red]Error: Interactive mode requires auto-complete service to be registered.[/]");
                    return ExitCodes.GeneralError;
                }

                var interactiveMode = new InteractiveMode(
                    _autoCompleteService,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<InteractiveMode>.Instance);

                try
                {
                    settings = await interactiveMode.BuildSearchSettingsAsync(settings);
                }
                catch (OperationCanceledException)
                {
                    AnsiConsole.MarkupLine("[yellow]Search cancelled.[/]");
                    return ExitCodes.Success;
                }
            }

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

            // Display search summary
            if (settings.Format.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                DisplaySearchSummary(result);
            }

            // Format and display results
            var formatter = _formatters[settings.Format];
            
            // Create simplified output objects for display
            var displayResults = result.Listings.Select(lr => new
            {
                Status = lr.Listing.IsNewListing ? "[green][NEW][/]" : "",
                Make = lr.Listing.Make,
                Model = lr.Listing.Model,
                Year = lr.Listing.Year,
                Price = FormatPriceWithChange(lr),
                Mileage = lr.Listing.Mileage.HasValue ? $"{lr.Listing.Mileage:N0} km" : "N/A",
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

    private void DisplaySearchSummary(SearchResult result)
    {
        var panel = new Panel(
            new Markup($"[bold]Total:[/] {result.TotalCount} listings  " +
                      $"[bold green]New:[/] {result.NewListingsCount}  " +
                      $"[bold yellow]Price Changes:[/] {result.PriceChangesCount}"))
        {
            Header = new PanelHeader("[bold]Search Results Summary[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private string FormatPriceWithChange(ListingSearchResult lr)
    {
        var priceStr = $"${lr.Listing.Price:N0}";
        
        if (lr.PriceChange != null)
        {
            var change = lr.PriceChange.PriceChange;
            var percentage = lr.PriceChange.ChangePercentage;
            
            if (change < 0)
            {
                // Price decreased - show in green
                return $"{priceStr} [green]↓${Math.Abs(change):N0} ({percentage:+0.0;-0.0}%)[/]";
            }
            else if (change > 0)
            {
                // Price increased - show in red
                return $"{priceStr} [red]↑${change:N0} ({percentage:+0.0;-0.0}%)[/]";
            }
        }
        
        return priceStr;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("-i|--interactive")]
        [Description("Enable interactive mode for guided search parameter selection")]
        [DefaultValue(false)]
        public bool Interactive { get; set; }

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

        [CommandOption("-p|--postal-code")]
        [Description("Canadian postal code for location search (e.g., M5V 3L9)")]
        public string? ZipCode { get; set; }

        [CommandOption("-r|--radius")]
        [Description("Search radius in kilometers (default: 40)")]
        [DefaultValue(40)]
        public double Radius { get; set; } = 40;

        [CommandOption("-f|--format")]
        [Description("Output format: table, json, csv (default: table)")]
        [DefaultValue("table")]
        public string Format { get; set; } = "table";

        [CommandOption("--page")]
        [Description("Page number (default: 1)")]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        [CommandOption("--page-size")]
        [Description("Results per page (default: 30)")]
        [DefaultValue(30)]
        public int PageSize { get; set; } = 30;
    }
}
