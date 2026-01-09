using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to display detailed information about a specific listing.
/// </summary>
public class ShowCommand : AsyncCommand<ShowCommand.Settings>
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IStatisticsService _statisticsService;

    public ShowCommand(
        IAutomatedMarketIntelligenceToolContext context,
        IStatisticsService statisticsService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Parse listing ID
            if (!Guid.TryParse(settings.ListingId, out var listingId))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid listing ID '{settings.ListingId}'[/]");
                return ExitCodes.ValidationError;
            }

            // Fetch listing
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.ListingId == new ListingId(listingId));

            if (listing == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Listing '{settings.ListingId}' not found[/]");
                return ExitCodes.NotFound;
            }

            DisplayListing(listing);

            // Calculate and display deal rating
            if (listing.Mileage.HasValue)
            {
                var dealRating = await _statisticsService.CalculateDealRating(
                    listing.Price,
                    settings.TenantId,
                    listing.Make,
                    listing.Model,
                    listing.Year,
                    listing.Mileage);

                var pricePerMile = await _statisticsService.CalculatePricePerMile(listing.Price, listing.Mileage);

                AnsiConsole.WriteLine();
                var analysisTable = new Table();
                analysisTable.Border(TableBorder.Rounded);
                analysisTable.AddColumn("[bold]Analysis[/]");
                analysisTable.AddColumn("[bold]Value[/]");

                var ratingColor = dealRating switch
                {
                    "Great" => "green",
                    "Good" => "blue",
                    "Fair" => "yellow",
                    _ => "red"
                };
                analysisTable.AddRow("Deal Rating", $"[{ratingColor}]{dealRating}[/]");

                if (pricePerMile.HasValue)
                    analysisTable.AddRow("Price per Mile", $"${pricePerMile.Value:F2}");

                AnsiConsole.Write(analysisTable);
            }

            // Show price history if requested
            if (settings.WithHistory)
            {
                await DisplayPriceHistory(listing);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private static void DisplayListing(Listing listing)
    {
        var panel = new Panel($"[bold]{listing.Year} {listing.Make} {listing.Model}[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Basic Information
        var basicTable = new Table();
        basicTable.Border(TableBorder.Rounded);
        basicTable.AddColumn("[bold]Field[/]");
        basicTable.AddColumn("[bold]Value[/]");

        basicTable.AddRow("Listing ID", listing.ListingId.Value.ToString());
        basicTable.AddRow("Source", listing.SourceSite);
        basicTable.AddRow("Price", $"[green]${listing.Price:N2}[/] {listing.Currency}");
        
        if (listing.Mileage.HasValue)
            basicTable.AddRow("Mileage", $"{listing.Mileage.Value:N0} mi");
        
        if (!string.IsNullOrEmpty(listing.Trim))
            basicTable.AddRow("Trim", listing.Trim);
        
        if (!string.IsNullOrEmpty(listing.Vin))
            basicTable.AddRow("VIN", listing.Vin);
        
        basicTable.AddRow("Condition", listing.Condition.ToString());
        
        if (listing.Transmission.HasValue)
            basicTable.AddRow("Transmission", listing.Transmission.Value.ToString());
        
        if (listing.FuelType.HasValue)
            basicTable.AddRow("Fuel Type", listing.FuelType.Value.ToString());
        
        if (listing.BodyStyle.HasValue)
            basicTable.AddRow("Body Style", listing.BodyStyle.Value.ToString());
        
        if (listing.Drivetrain.HasValue)
            basicTable.AddRow("Drivetrain", listing.Drivetrain.Value.ToString());
        
        if (!string.IsNullOrEmpty(listing.ExteriorColor))
            basicTable.AddRow("Exterior Color", listing.ExteriorColor);
        
        if (!string.IsNullOrEmpty(listing.InteriorColor))
            basicTable.AddRow("Interior Color", listing.InteriorColor);

        AnsiConsole.Write(basicTable);
        AnsiConsole.WriteLine();

        // Location
        if (!string.IsNullOrEmpty(listing.City) || !string.IsNullOrEmpty(listing.Province))
        {
            AnsiConsole.MarkupLine("[bold]Location:[/]");
            var location = $"{listing.City}, {listing.Province}";
            if (!string.IsNullOrEmpty(listing.PostalCode))
                location += $" {listing.PostalCode}";
            AnsiConsole.MarkupLine($"  {location}");
            AnsiConsole.WriteLine();
        }

        // Seller
        if (!string.IsNullOrEmpty(listing.SellerName))
        {
            AnsiConsole.MarkupLine("[bold]Seller:[/]");
            AnsiConsole.MarkupLine($"  Name: {listing.SellerName}");
            if (listing.SellerType.HasValue)
                AnsiConsole.MarkupLine($"  Type: {listing.SellerType.Value}");
            if (!string.IsNullOrEmpty(listing.SellerPhone))
                AnsiConsole.MarkupLine($"  Phone: {listing.SellerPhone}");
            AnsiConsole.WriteLine();
        }

        // Description
        if (!string.IsNullOrEmpty(listing.Description))
        {
            AnsiConsole.MarkupLine("[bold]Description:[/]");
            var descPanel = new Panel(listing.Description)
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1)
            };
            AnsiConsole.Write(descPanel);
            AnsiConsole.WriteLine();
        }

        // Images
        if (listing.ImageUrls.Any())
        {
            AnsiConsole.MarkupLine($"[bold]Images:[/] {listing.ImageUrls.Count} available");
            foreach (var url in listing.ImageUrls.Take(5))
            {
                AnsiConsole.MarkupLine($"  {url}");
            }
            if (listing.ImageUrls.Count > 5)
                AnsiConsole.MarkupLine($"  ... and {listing.ImageUrls.Count - 5} more");
            AnsiConsole.WriteLine();
        }

        // Listing URL
        AnsiConsole.MarkupLine($"[bold]Listing URL:[/] [link]{listing.ListingUrl}[/]");
        AnsiConsole.WriteLine();

        // Dates
        var datesTable = new Table();
        datesTable.Border(TableBorder.Rounded);
        datesTable.AddColumn("[bold]Date[/]");
        datesTable.AddColumn("[bold]Value[/]");

        if (listing.ListingDate.HasValue)
            datesTable.AddRow("Listed", listing.ListingDate.Value.ToString("yyyy-MM-dd"));
        
        if (listing.DaysOnMarket.HasValue)
            datesTable.AddRow("Days on Market", listing.DaysOnMarket.Value.ToString());
        
        datesTable.AddRow("First Seen", listing.FirstSeenDate.ToString("yyyy-MM-dd HH:mm"));
        datesTable.AddRow("Last Seen", listing.LastSeenDate.ToString("yyyy-MM-dd HH:mm"));
        datesTable.AddRow("Status", listing.IsActive ? "[green]Active[/]" : "[red]Inactive[/]");

        if (listing.IsNewListing)
            datesTable.AddRow("", "[green]★ NEW[/]");

        AnsiConsole.Write(datesTable);
    }

    private async Task DisplayPriceHistory(Listing listing)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Price History:[/]");

        var priceHistory = await _context.PriceHistory
            .Where(ph => ph.ListingId == listing.ListingId)
            .OrderByDescending(ph => ph.ObservedAt)
            .Take(10)
            .ToListAsync();

        if (!priceHistory.Any())
        {
            AnsiConsole.MarkupLine("  [yellow]No price history available[/]");
            return;
        }

        var historyTable = new Table();
        historyTable.Border(TableBorder.Rounded);
        historyTable.AddColumn("Date");
        historyTable.AddColumn("Price");
        historyTable.AddColumn("Change");

        decimal? previousPrice = null;
        foreach (var history in priceHistory.AsEnumerable().Reverse())
        {
            var changeText = "-";
            if (previousPrice.HasValue)
            {
                var change = history.Price - previousPrice.Value;
                var changePercent = (change / previousPrice.Value) * 100;
                
                if (change > 0)
                    changeText = $"[red]+${change:N2} (+{changePercent:F1}%)[/]";
                else if (change < 0)
                    changeText = $"[green]${change:N2} ({changePercent:F1}%)[/]";
                else
                    changeText = "[gray]No change[/]";
            }

            historyTable.AddRow(
                history.ObservedAt.ToString("yyyy-MM-dd HH:mm"),
                $"${history.Price:N2}",
                changeText
            );

            previousPrice = history.Price;
        }

        AnsiConsole.Write(historyTable);
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<listing-id>")]
        [Description("Listing ID to display")]
        public string ListingId { get; set; } = string.Empty;

        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("--with-history")]
        [Description("Include price history")]
        public bool WithHistory { get; set; }
    }
}
