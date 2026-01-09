using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage the watch list.
/// </summary>
public class WatchCommand : AsyncCommand<WatchCommand.Settings>
{
    private readonly IWatchListService _watchListService;
    private readonly ILogger<WatchCommand> _logger;

    public WatchCommand(
        IWatchListService watchListService,
        ILogger<WatchCommand> logger)
    {
        _watchListService = watchListService ?? throw new ArgumentNullException(nameof(watchListService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.Action.ToLowerInvariant() switch
            {
                "add" => await AddToWatchListAsync(settings),
                "remove" => await RemoveFromWatchListAsync(settings),
                "list" => await ListWatchedListingsAsync(settings),
                _ => await ListWatchedListingsAsync(settings)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Watch command failed: {ErrorMessage}", ex.Message);
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> AddToWatchListAsync(Settings settings)
    {
        if (settings.ListingId == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Listing ID is required for 'add' action[/]");
            return ExitCodes.ValidationError;
        }

        var tenantId = settings.TenantId ?? Guid.Empty;
        var listingId = new ListingId(settings.ListingId.Value);

        var watchedListing = await _watchListService.AddToWatchListAsync(
            tenantId,
            listingId,
            settings.Notes,
            settings.NotifyOnPriceChange,
            settings.NotifyOnRemoval);

        AnsiConsole.MarkupLine($"[green]✓ Added listing to watch list[/]");
        AnsiConsole.MarkupLine($"  Listing ID: {listingId.Value}");
        if (!string.IsNullOrEmpty(settings.Notes))
        {
            AnsiConsole.MarkupLine($"  Notes: {settings.Notes}");
        }

        return ExitCodes.Success;
    }

    private async Task<int> RemoveFromWatchListAsync(Settings settings)
    {
        if (settings.ListingId == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Listing ID is required for 'remove' action[/]");
            return ExitCodes.ValidationError;
        }

        var tenantId = settings.TenantId ?? Guid.Empty;
        var listingId = new ListingId(settings.ListingId.Value);

        await _watchListService.RemoveFromWatchListAsync(tenantId, listingId);

        AnsiConsole.MarkupLine($"[green]✓ Removed listing from watch list[/]");
        return ExitCodes.Success;
    }

    private async Task<int> ListWatchedListingsAsync(Settings settings)
    {
        var tenantId = settings.TenantId ?? Guid.Empty;
        var watchedListings = await _watchListService.GetAllWatchedListingsAsync(tenantId);

        if (!watchedListings.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No listings in watch list[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Listing ID");
        table.AddColumn("Vehicle");
        table.AddColumn("Price");
        table.AddColumn("Status");
        table.AddColumn("Added");
        table.AddColumn("Notes");

        foreach (var watchedListing in watchedListings)
        {
            var listing = watchedListing.Listing;
            var vehicle = $"{listing.Year} {listing.Make} {listing.Model}";
            var price = $"${listing.Price:N2}";
            var status = listing.IsActive ? "[green]Active[/]" : "[red]Inactive[/]";
            var added = watchedListing.CreatedAt.ToString("yyyy-MM-dd");
            var notes = watchedListing.Notes ?? "";

            table.AddRow(
                listing.ListingId.Value.ToString()[..Math.Min(8, listing.ListingId.Value.ToString().Length)],
                vehicle,
                price,
                status,
                added,
                notes);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {watchedListings.Count} watched listings[/]");

        return ExitCodes.Success;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action to perform (list, add, remove)")]
        [DefaultValue("list")]
        public string Action { get; set; } = "list";

        [CommandOption("--listing-id")]
        [Description("ID of the listing")]
        public Guid? ListingId { get; set; }

        [CommandOption("--notes")]
        [Description("Notes about the watched listing")]
        public string? Notes { get; set; }

        [CommandOption("--notify-price")]
        [Description("Notify on price change")]
        [DefaultValue(true)]
        public bool NotifyOnPriceChange { get; set; } = true;

        [CommandOption("--notify-removal")]
        [Description("Notify when listing is removed")]
        [DefaultValue(true)]
        public bool NotifyOnRemoval { get; set; } = true;

        [CommandOption("--tenant-id")]
        [Description("Tenant ID (defaults to Guid.Empty for single-tenant mode)")]
        public Guid? TenantId { get; set; }
    }
}
