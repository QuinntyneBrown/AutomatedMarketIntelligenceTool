using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to display system status including scraper health and statistics summary.
/// </summary>
public class StatusCommand : AsyncCommand<StatusCommand.Settings>
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IStatisticsService _statisticsService;

    public StatusCommand(
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
            var panel = new Panel("[bold]System Status[/]")
            {
                Border = BoxBorder.Double
            };
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Database Statistics
            await DisplayDatabaseStatistics(settings.TenantId);

            // Recent Activity
            await DisplayRecentActivity(settings.TenantId);

            // Source Site Statistics
            await DisplaySourceSiteStatistics(settings.TenantId);

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task DisplayDatabaseStatistics(Guid tenantId)
    {
        AnsiConsole.MarkupLine("[bold]Database Statistics:[/]");

        var totalListings = await _context.Listings.CountAsync();
        var activeListings = await _context.Listings.CountAsync(l => l.IsActive);
        var newListings = await _context.Listings.CountAsync(l => l.IsNewListing);
        var totalVehicles = await _context.Vehicles.CountAsync();
        var totalProfiles = await _context.SearchProfiles.CountAsync();

        var statsTable = new Table();
        statsTable.Border(TableBorder.Rounded);
        statsTable.AddColumn("[bold]Metric[/]");
        statsTable.AddColumn("[bold]Count[/]");

        statsTable.AddRow("Total Listings", $"{totalListings:N0}");
        statsTable.AddRow("Active Listings", $"[green]{activeListings:N0}[/]");
        statsTable.AddRow("New Listings", $"[blue]{newListings:N0}[/]");
        statsTable.AddRow("Unique Vehicles", $"{totalVehicles:N0}");
        statsTable.AddRow("Saved Profiles", $"{totalProfiles:N0}");

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();
    }

    private async Task DisplayRecentActivity(Guid tenantId)
    {
        AnsiConsole.MarkupLine("[bold]Recent Activity (Last 24 Hours):[/]");

        var yesterday = DateTime.UtcNow.AddDays(-1);
        
        var listingsAdded = await _context.Listings
            .CountAsync(l => l.CreatedAt >= yesterday);
        
        var listingsUpdated = await _context.Listings
            .CountAsync(l => l.UpdatedAt.HasValue && l.UpdatedAt.Value >= yesterday);
        
        var priceChanges = await _context.PriceHistory
            .CountAsync(ph => ph.ObservedAt >= yesterday);

        var activityTable = new Table();
        activityTable.Border(TableBorder.Rounded);
        activityTable.AddColumn("[bold]Activity[/]");
        activityTable.AddColumn("[bold]Count[/]");

        activityTable.AddRow("New Listings", $"[green]{listingsAdded:N0}[/]");
        activityTable.AddRow("Updated Listings", $"[blue]{listingsUpdated:N0}[/]");
        activityTable.AddRow("Price Changes", $"[yellow]{priceChanges:N0}[/]");

        AnsiConsole.Write(activityTable);
        AnsiConsole.WriteLine();
    }

    private async Task DisplaySourceSiteStatistics(Guid tenantId)
    {
        AnsiConsole.MarkupLine("[bold]Listings by Source:[/]");

        var sourceCounts = await _context.Listings
            .Where(l => l.IsActive)
            .GroupBy(l => l.SourceSite)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        if (!sourceCounts.Any())
        {
            AnsiConsole.MarkupLine("  [yellow]No data available[/]");
            return;
        }

        var sourceTable = new Table();
        sourceTable.Border(TableBorder.Rounded);
        sourceTable.AddColumn("[bold]Source[/]");
        sourceTable.AddColumn("[bold]Active Listings[/]");
        sourceTable.AddColumn("[bold]Percentage[/]");

        var total = sourceCounts.Sum(x => x.Count);
        
        foreach (var source in sourceCounts)
        {
            var percentage = (source.Count * 100.0) / total;
            sourceTable.AddRow(
                source.Source,
                source.Count.ToString("N0"),
                $"{percentage:F1}%"
            );
        }

        AnsiConsole.Write(sourceTable);
        AnsiConsole.WriteLine();

        // Display as bar chart if multiple sources
        if (sourceCounts.Count > 1)
        {
            var chart = new BarChart()
                .Width(60)
                .Label("[bold]Source Distribution[/]");

            foreach (var source in sourceCounts)
            {
                chart.AddItem(source.Source, source.Count, Color.Green);
            }

            AnsiConsole.Write(chart);
        }
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("-s|--site <SITE>")]
        [Description("Filter status by specific source site")]
        public string? Site { get; set; }
    }
}
