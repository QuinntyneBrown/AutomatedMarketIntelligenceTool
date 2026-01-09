using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to compare multiple listings side-by-side.
/// </summary>
public class CompareCommand : AsyncCommand<CompareCommand.Settings>
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<CompareCommand> _logger;

    public CompareCommand(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<CompareCommand> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            if (settings.ListingIds == null || !settings.ListingIds.Any())
            {
                AnsiConsole.MarkupLine("[red]Error: At least one listing ID is required[/]");
                return ExitCodes.ValidationError;
            }

            if (settings.ListingIds.Count() > 5)
            {
                AnsiConsole.MarkupLine("[red]Error: Maximum 5 listings can be compared at once[/]");
                return ExitCodes.ValidationError;
            }

            return await CompareListingsAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compare command failed: {ErrorMessage}", ex.Message);
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> CompareListingsAsync(Settings settings)
    {
        var tenantId = settings.TenantId ?? Guid.Empty;
        var listingIds = settings.ListingIds!.Select(id => new ListingId(id)).ToList();

        // Fetch all listings
        var listings = new List<Listing>();
        foreach (var listingId in listingIds)
        {
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.ListingId == listingId);

            if (listing == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Listing {listingId.Value} not found[/]");
                return ExitCodes.NotFound;
            }

            listings.Add(listing);
        }

        // Display comparison table
        DisplayComparisonTable(listings);

        // Display best values
        DisplayBestValues(listings);

        return ExitCodes.Success;
    }

    private void DisplayComparisonTable(List<Listing> listings)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Title("[bold yellow]Listing Comparison[/]");
        
        // Add column for field names
        table.AddColumn("[bold]Field[/]");
        
        // Add column for each listing
        for (int i = 0; i < listings.Count; i++)
        {
            table.AddColumn($"[bold]Listing {i + 1}[/]");
        }

        // Add rows for each field
        AddComparisonRow(table, "ID", listings.Select(l => l.ListingId.Value.ToString().Substring(0, 8)).ToArray());
        AddComparisonRow(table, "Make", listings.Select(l => l.Make).ToArray());
        AddComparisonRow(table, "Model", listings.Select(l => l.Model).ToArray());
        AddComparisonRow(table, "Year", listings.Select(l => l.Year.ToString()).ToArray());
        AddComparisonRow(table, "Price", listings.Select(l => $"${l.Price:N2}").ToArray());
        AddComparisonRow(table, "Mileage", listings.Select(l => l.Mileage?.ToString("N0") ?? "N/A").ToArray());
        AddComparisonRow(table, "VIN", listings.Select(l => l.Vin ?? "N/A").ToArray());
        AddComparisonRow(table, "Condition", listings.Select(l => l.Condition.ToString()).ToArray());
        AddComparisonRow(table, "Transmission", listings.Select(l => l.Transmission?.ToString() ?? "N/A").ToArray());
        AddComparisonRow(table, "Fuel Type", listings.Select(l => l.FuelType?.ToString() ?? "N/A").ToArray());
        AddComparisonRow(table, "Body Style", listings.Select(l => l.BodyStyle?.ToString() ?? "N/A").ToArray());
        AddComparisonRow(table, "Location", listings.Select(l => $"{l.City}, {l.Province}").ToArray());
        AddComparisonRow(table, "Dealer", listings.Select(l => l.SellerName ?? "N/A").ToArray());
        AddComparisonRow(table, "Status", listings.Select(l => l.IsActive ? "Active" : "Inactive").ToArray());
        AddComparisonRow(table, "Source", listings.Select(l => l.SourceSite).ToArray());
        AddComparisonRow(table, "URL", listings.Select(l => TruncateUrl(l.ListingUrl)).ToArray());

        AnsiConsole.Write(table);
    }

    private void AddComparisonRow(Table table, string fieldName, string[] values)
    {
        var row = new List<string> { fieldName };
        
        // Highlight differences
        var uniqueValues = values.Distinct().Count();
        var hasDifference = uniqueValues > 1;

        foreach (var value in values)
        {
            if (hasDifference && fieldName != "ID" && fieldName != "URL")
            {
                row.Add($"[yellow]{value}[/]");
            }
            else
            {
                row.Add(value);
            }
        }

        table.AddRow(row.ToArray());
    }

    private void DisplayBestValues(List<Listing> listings)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(new Markup(GetBestValuesMarkup(listings)))
        {
            Header = new PanelHeader("[bold green]Best Values[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
    }

    private string GetBestValuesMarkup(List<Listing> listings)
    {
        var markup = "";

        // Lowest price
        var lowestPrice = listings.MinBy(l => l.Price);
        markup += $"[green]✓ Lowest Price:[/] ${lowestPrice!.Price:N2} ({lowestPrice.Year} {lowestPrice.Make} {lowestPrice.Model})\n";

        // Lowest mileage
        var withMileage = listings.Where(l => l.Mileage.HasValue).ToList();
        if (withMileage.Any())
        {
            var lowestMileage = withMileage.MinBy(l => l.Mileage!.Value);
            markup += $"[green]✓ Lowest Mileage:[/] {lowestMileage!.Mileage:N0} miles ({lowestMileage.Year} {lowestMileage.Make} {lowestMileage.Model})\n";
        }

        // Newest year
        var newestYear = listings.MaxBy(l => l.Year);
        markup += $"[green]✓ Newest Year:[/] {newestYear!.Year} ({newestYear.Make} {newestYear.Model})";

        return markup;
    }

    private string TruncateUrl(string url)
    {
        if (url.Length <= 40)
            return url;
        
        return url.Substring(0, 37) + "...";
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<listing-ids>")]
        [Description("IDs of the listings to compare (space-separated, max 5)")]
        public IEnumerable<Guid>? ListingIds { get; set; }

        [CommandOption("--tenant-id")]
        [Description("Tenant ID (defaults to Guid.Empty for single-tenant mode)")]
        public Guid? TenantId { get; set; }
    }
}
