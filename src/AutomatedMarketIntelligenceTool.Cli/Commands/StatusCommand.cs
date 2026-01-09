using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;
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
    private readonly IScraperHealthService? _healthService;

    public StatusCommand(
        IAutomatedMarketIntelligenceToolContext context,
        IStatisticsService statisticsService,
        IScraperHealthService? healthService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _healthService = healthService;
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

            // Scraper Health (if service available)
            if (_healthService != null)
            {
                await DisplayScraperHealth(settings.Site);
            }

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

    private async Task DisplayScraperHealth(string? filterSite)
    {
        if (_healthService == null)
            return;

        AnsiConsole.MarkupLine("[bold]Scraper Health Monitoring:[/]");

        var allMetrics = _healthService.GetAllHealthMetrics();
        
        if (filterSite != null)
        {
            allMetrics = allMetrics
                .Where(kvp => kvp.Key.Equals(filterSite, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        if (!allMetrics.Any())
        {
            AnsiConsole.MarkupLine("  [yellow]No health data available yet[/]");
            return;
        }

        var healthTable = new Table();
        healthTable.Border(TableBorder.Rounded);
        healthTable.AddColumn("[bold]Site[/]");
        healthTable.AddColumn("[bold]Status[/]");
        healthTable.AddColumn("[bold]Success Rate[/]");
        healthTable.AddColumn("[bold]Listings[/]");
        healthTable.AddColumn("[bold]Avg Response[/]");
        healthTable.AddColumn("[bold]Last Attempt[/]");

        foreach (var (siteName, metrics) in allMetrics.OrderBy(kvp => kvp.Key))
        {
            var status = metrics.GetHealthStatus();
            var statusColor = status switch
            {
                ScraperHealthStatus.Healthy => "green",
                ScraperHealthStatus.Degraded => "yellow",
                ScraperHealthStatus.Failed => "red",
                _ => "white"
            };

            var successRateColor = metrics.SuccessRate >= 80 ? "green" :
                                  metrics.SuccessRate >= 50 ? "yellow" : "red";

            var lastAttempt = metrics.LastAttemptedAt != default 
                ? GetRelativeTime(metrics.LastAttemptedAt)
                : "Never";

            healthTable.AddRow(
                siteName,
                $"[{statusColor}]{status}[/]",
                $"[{successRateColor}]{metrics.SuccessRate:F1}%[/] ({metrics.SuccessfulAttempts}/{metrics.TotalAttempts})",
                metrics.ListingsFound.ToString("N0"),
                $"{metrics.AverageResponseTime:N0}ms",
                lastAttempt
            );
        }

        AnsiConsole.Write(healthTable);
        AnsiConsole.WriteLine();

        // Display warnings for degraded/failed scrapers
        var problemScrapers = allMetrics
            .Where(kvp => kvp.Value.GetHealthStatus() != ScraperHealthStatus.Healthy)
            .ToList();

        if (problemScrapers.Any())
        {
            AnsiConsole.MarkupLine("[bold yellow]⚠ Health Warnings:[/]");
            
            foreach (var (siteName, metrics) in problemScrapers)
            {
                if (metrics.HasZeroResults)
                {
                    AnsiConsole.MarkupLine($"  [yellow]• {siteName}: Zero results detected - possible scraper breakage[/]");
                }
                
                if (metrics.MissingElementCount > 0)
                {
                    AnsiConsole.MarkupLine($"  [yellow]• {siteName}: {metrics.MissingElementCount} missing elements detected[/]");
                    foreach (var element in metrics.MissingElements.Take(3))
                    {
                        AnsiConsole.MarkupLine($"    - {element}");
                    }
                    if (metrics.MissingElements.Count > 3)
                    {
                        AnsiConsole.MarkupLine($"    - ... and {metrics.MissingElements.Count - 3} more");
                    }
                }

                if (metrics.GetHealthStatus() == ScraperHealthStatus.Failed)
                {
                    AnsiConsole.MarkupLine($"  [red]• {siteName}: Scraper may need updates (success rate: {metrics.SuccessRate:F1}%)[/]");
                    if (!string.IsNullOrEmpty(metrics.LastError))
                    {
                        AnsiConsole.MarkupLine($"    Last error: {metrics.LastError}");
                    }
                }
            }
            
            AnsiConsole.WriteLine();
        }
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        
        return dateTime.ToShortDateString();
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
