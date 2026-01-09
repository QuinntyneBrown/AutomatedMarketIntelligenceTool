using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Cli.Formatters;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to list saved car listings from the database with filtering and pagination.
/// </summary>
public class ListCommand : AsyncCommand<ListCommand.Settings>
{
    private readonly ISearchService _searchService;
    private readonly Dictionary<string, IOutputFormatter> _formatters;

    public ListCommand(ISearchService searchService)
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

            // Parse enums from string arrays
            Condition[]? conditions = null;
            if (settings.Conditions?.Length > 0)
            {
                conditions = ParseEnums<Condition>(settings.Conditions, "condition");
                if (conditions == null) return ExitCodes.ValidationError;
            }

            Transmission[]? transmissions = null;
            if (settings.Transmissions?.Length > 0)
            {
                transmissions = ParseEnums<Transmission>(settings.Transmissions, "transmission");
                if (transmissions == null) return ExitCodes.ValidationError;
            }

            FuelType[]? fuelTypes = null;
            if (settings.FuelTypes?.Length > 0)
            {
                fuelTypes = ParseEnums<FuelType>(settings.FuelTypes, "fuel type");
                if (fuelTypes == null) return ExitCodes.ValidationError;
            }

            BodyStyle[]? bodyStyles = null;
            if (settings.BodyStyles?.Length > 0)
            {
                bodyStyles = ParseEnums<BodyStyle>(settings.BodyStyles, "body style");
                if (bodyStyles == null) return ExitCodes.ValidationError;
            }

            SearchSortField? sortBy = null;
            if (!string.IsNullOrEmpty(settings.SortBy))
            {
                if (!Enum.TryParse<SearchSortField>(settings.SortBy, true, out var parsedSort))
                {
                    AnsiConsole.MarkupLine($"[red]Error: Invalid sort field '{settings.SortBy}'. Valid values are: Price, Year, Mileage, Distance, CreatedAt[/]");
                    return ExitCodes.ValidationError;
                }
                sortBy = parsedSort;
            }

            var sortDirection = settings.SortDescending ? SortDirection.Descending : SortDirection.Ascending;

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
                Conditions = conditions,
                Transmissions = transmissions,
                FuelTypes = fuelTypes,
                BodyStyles = bodyStyles,
                City = settings.City,
                Province = settings.State,
                PostalCode = settings.PostalCode,
                RadiusKilometers = settings.Radius,
                Page = settings.Page,
                PageSize = settings.PageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
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

            // Filter for new listings only if requested
            var listings = result.Listings;
            if (settings.NewOnly)
            {
                listings = listings.Where(lr => lr.Listing.IsNewListing).ToList();
            }

            if (listings.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No listings found matching the criteria.[/]");
                return ExitCodes.Success;
            }

            // Display search summary
            if (settings.Format.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                DisplaySearchSummary(result);
            }

            // Format and display results
            var formatter = _formatters[settings.Format];
            
            // Create simplified output objects for display
            var displayResults = listings.Select(lr => new
            {
                Status = lr.Listing.IsNewListing ? "[green][NEW][/]" : "",
                Year = lr.Listing.Year,
                Make = lr.Listing.Make,
                Model = lr.Listing.Model,
                Price = FormatPriceWithChange(lr),
                Mileage = lr.Listing.Mileage.HasValue ? $"{lr.Listing.Mileage:N0} km" : "N/A",
                Location = $"{lr.Listing.City}, {lr.Listing.Province}",
                Condition = lr.Listing.Condition.ToString(),
                Source = lr.Listing.SourceSite,
                Distance = lr.DistanceKilometers.HasValue ? $"{lr.DistanceKilometers:F1} km" : "N/A"
            });

            formatter.Format(displayResults);

            // Show pagination info
            if (settings.Format.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"\n[dim]Page {result.Page} of {result.TotalPages} ({listings.Count} of {result.TotalCount} listings)[/]");
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private T[]? ParseEnums<T>(string[] values, string fieldName) where T : struct, Enum
    {
        var results = new List<T>();
        foreach (var value in values)
        {
            if (!Enum.TryParse<T>(value, true, out var parsed))
            {
                var validValues = string.Join(", ", Enum.GetNames(typeof(T)));
                AnsiConsole.MarkupLine($"[red]Error: Invalid {fieldName} '{value}'. Valid values are: {validValues}[/]");
                return null;
            }
            results.Add(parsed);
        }
        return results.ToArray();
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

        [CommandOption("-m|--make")]
        [Description("Vehicle make(s) (e.g., Toyota, Honda)")]
        public string[]? Makes { get; set; }

        [CommandOption("--model")]
        [Description("Vehicle model(s) (e.g., Camry, Civic)")]
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

        [CommandOption("--condition")]
        [Description("Condition(s): New, Used, Certified")]
        public string[]? Conditions { get; set; }

        [CommandOption("--transmission")]
        [Description("Transmission(s): Automatic, Manual, CVT")]
        public string[]? Transmissions { get; set; }

        [CommandOption("--fuel-type")]
        [Description("Fuel type(s): Gasoline, Diesel, Electric, Hybrid, PlugInHybrid")]
        public string[]? FuelTypes { get; set; }

        [CommandOption("--body-style")]
        [Description("Body style(s): Sedan, SUV, Truck, Coupe, Hatchback, Wagon, Van, Convertible")]
        public string[]? BodyStyles { get; set; }

        [CommandOption("--city")]
        [Description("City name")]
        public string? City { get; set; }

        [CommandOption("--state")]
        [Description("Province/State")]
        public string? State { get; set; }

        [CommandOption("-p|--postal-code")]
        [Description("Postal code for location search")]
        public string? PostalCode { get; set; }

        [CommandOption("-r|--radius")]
        [Description("Search radius in kilometers (default: 40)")]
        [DefaultValue(40)]
        public double Radius { get; set; } = 40;

        [CommandOption("--new-only")]
        [Description("Show only new listings")]
        [DefaultValue(false)]
        public bool NewOnly { get; set; }

        [CommandOption("--sort")]
        [Description("Sort by field: Price, Year, Mileage, Distance, CreatedAt")]
        public string? SortBy { get; set; }

        [CommandOption("--sort-desc")]
        [Description("Sort in descending order")]
        [DefaultValue(false)]
        public bool SortDescending { get; set; }

        [CommandOption("-f|--format")]
        [Description("Output format: table, json, csv (default: table)")]
        [DefaultValue("table")]
        public string Format { get; set; } = "table";

        [CommandOption("--page")]
        [Description("Page number (default: 1)")]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        [CommandOption("--page-size")]
        [Description("Results per page (default: 25)")]
        [DefaultValue(25)]
        public int PageSize { get; set; } = 25;
    }
}
