using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Services.Dashboard;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to display a real-time dashboard with market insights and tracking summary.
/// </summary>
public class DashboardCommand : AsyncCommand<DashboardCommand.Settings>
{
    private readonly IDashboardService _dashboardService;
    private readonly IScraperHealthService? _healthService;
    private readonly SignalHandler _signalHandler;

    public DashboardCommand(
        IDashboardService dashboardService,
        SignalHandler signalHandler,
        IScraperHealthService? healthService = null)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _signalHandler = signalHandler ?? throw new ArgumentNullException(nameof(signalHandler));
        _healthService = healthService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            if (settings.Watch)
            {
                return await RunWatchModeAsync(settings);
            }
            else
            {
                await DisplayDashboardAsync(settings);
                return ExitCodes.Success;
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Dashboard interrupted.[/]");
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> RunWatchModeAsync(Settings settings)
    {
        var refreshInterval = TimeSpan.FromSeconds(settings.RefreshInterval);
        var cancellationToken = _signalHandler.CancellationToken;

        AnsiConsole.MarkupLine($"[dim]Dashboard watch mode active. Refreshing every {settings.RefreshInterval} seconds. Press Ctrl+C to exit.[/]");
        AnsiConsole.WriteLine();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Clear console for fresh display
                AnsiConsole.Clear();

                // Display header with timestamp
                DisplayWatchModeHeader(settings.RefreshInterval);

                // Display the dashboard
                await DisplayDashboardAsync(settings, cancellationToken);

                // Wait for next refresh or cancellation
                await Task.Delay(refreshInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal exit from watch mode
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Watch mode ended.[/]");
        return ExitCodes.Success;
    }

    private static void DisplayWatchModeHeader(int refreshInterval)
    {
        var rule = new Rule($"[bold blue]Dashboard[/] [dim](Auto-refresh: {refreshInterval}s | Last update: {DateTime.Now:HH:mm:ss})[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    private async Task DisplayDashboardAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        var data = await _dashboardService.GetDashboardDataAsync(
            settings.TenantId,
            settings.TrendDays,
            cancellationToken);

        if (settings.Compact)
        {
            DisplayCompactDashboard(data);
        }
        else
        {
            DisplayFullDashboard(data, settings);
        }
    }

    private void DisplayCompactDashboard(DashboardData data)
    {
        // Single compact table with key metrics
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Dashboard Summary[/]");

        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");
        table.AddColumn("[bold]Trend[/]");

        // Listings
        table.AddRow(
            "Active Listings",
            $"[green]{data.ListingSummary.ActiveListings:N0}[/]",
            FormatTrend(data.MarketTrends.InventoryTrend));

        table.AddRow(
            "New Today",
            $"[blue]{data.ListingSummary.NewToday:N0}[/]",
            "");

        table.AddRow(
            "Price Drops Today",
            $"[green]{data.ListingSummary.PriceDropsToday:N0}[/]",
            "");

        // Watch list
        table.AddRow(
            "Watched Listings",
            $"{data.WatchListSummary.TotalWatched:N0}",
            data.WatchListSummary.WithPriceChanges > 0
                ? $"[yellow]{data.WatchListSummary.WithPriceChanges} changed[/]"
                : "");

        // Alerts
        table.AddRow(
            "Active Alerts",
            $"{data.AlertSummary.ActiveAlerts:N0}",
            data.AlertSummary.TriggeredToday > 0
                ? $"[yellow]{data.AlertSummary.TriggeredToday} triggered[/]"
                : "");

        // Average price
        table.AddRow(
            "Avg Price",
            $"${data.MarketTrends.AveragePriceTrend.CurrentValue:N0}",
            FormatTrend(data.MarketTrends.AveragePriceTrend));

        AnsiConsole.Write(table);
    }

    private void DisplayFullDashboard(DashboardData data, Settings settings)
    {
        // Title panel
        var titlePanel = new Panel("[bold]Market Intelligence Dashboard[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(titlePanel);
        AnsiConsole.WriteLine();

        // Overview section using a grid layout
        DisplayListingsSummary(data.ListingSummary);
        AnsiConsole.WriteLine();

        DisplayMarketTrends(data.MarketTrends);
        AnsiConsole.WriteLine();

        DisplayWatchListAndAlerts(data.WatchListSummary, data.AlertSummary);
        AnsiConsole.WriteLine();

        DisplaySystemMetrics(data.SystemMetrics);

        // Scraper health if available
        if (_healthService != null && settings.ShowHealth)
        {
            AnsiConsole.WriteLine();
            DisplayScraperHealth();
        }

        // Footer with generation timestamp
        AnsiConsole.MarkupLine($"[dim]Generated at: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC[/]");
    }

    private static void DisplayListingsSummary(ListingSummary summary)
    {
        AnsiConsole.MarkupLine("[bold]Listings Overview[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .HideHeaders();

        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.AddColumn("Metric2");
        table.AddColumn("Value2");

        table.AddRow(
            "[dim]Total Listings[/]",
            $"{summary.TotalListings:N0}",
            "[dim]Active Listings[/]",
            $"[green]{summary.ActiveListings:N0}[/]");

        table.AddRow(
            "[dim]New Today[/]",
            $"[blue]{summary.NewToday:N0}[/]",
            "[dim]New This Week[/]",
            $"[blue]{summary.NewThisWeek:N0}[/]");

        table.AddRow(
            "[dim]Price Drops Today[/]",
            $"[green]↓ {summary.PriceDropsToday:N0}[/]",
            "[dim]Price Increases Today[/]",
            $"[red]↑ {summary.PriceIncreasesToday:N0}[/]");

        table.AddRow(
            "[dim]Deactivated Today[/]",
            $"[yellow]{summary.DeactivatedToday:N0}[/]",
            "[dim]Unique Vehicles[/]",
            $"{summary.UniqueVehicles:N0}");

        AnsiConsole.Write(table);

        // Source breakdown bar chart
        if (summary.BySource.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Active Listings by Source[/]");

            var chart = new BarChart()
                .Width(60)
                .Label("[bold]Sources[/]");

            var colors = new[] { Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Aqua };
            var colorIndex = 0;

            foreach (var source in summary.BySource.OrderByDescending(x => x.Value).Take(8))
            {
                chart.AddItem(source.Key, source.Value, colors[colorIndex % colors.Length]);
                colorIndex++;
            }

            AnsiConsole.Write(chart);
        }
    }

    private static void DisplayMarketTrends(MarketTrends trends)
    {
        AnsiConsole.MarkupLine($"[bold]Market Trends[/] [dim](Last {trends.TrendDays} days)[/]");

        var table = new Table()
            .Border(TableBorder.Rounded);

        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Current[/]");
        table.AddColumn("[bold]Previous[/]");
        table.AddColumn("[bold]Change[/]");

        // Average Price
        table.AddRow(
            "Average Price",
            $"${trends.AveragePriceTrend.CurrentValue:N0}",
            $"${trends.AveragePriceTrend.PreviousValue:N0}",
            FormatTrend(trends.AveragePriceTrend));

        // Inventory
        table.AddRow(
            "Inventory",
            $"{trends.InventoryTrend.CurrentValue:N0}",
            $"{trends.InventoryTrend.PreviousValue:N0}",
            FormatTrend(trends.InventoryTrend));

        // New Listings Rate
        table.AddRow(
            "New/Day",
            $"{trends.NewListingsRateTrend.CurrentValue:F1}",
            $"{trends.NewListingsRateTrend.PreviousValue:F1}",
            FormatTrend(trends.NewListingsRateTrend));

        // Days on Market
        table.AddRow(
            "Avg Days on Market",
            $"{trends.DaysOnMarketTrend.CurrentValue:F1}",
            $"{trends.DaysOnMarketTrend.PreviousValue:F1}",
            FormatTrend(trends.DaysOnMarketTrend, invertColor: true));

        AnsiConsole.Write(table);

        // Sparkline for daily new listings if data available
        if (trends.DailyNewListings.Count >= 7)
        {
            DisplaySparkline("Daily New Listings", trends.DailyNewListings.TakeLast(14).ToList());
        }
    }

    private static void DisplaySparkline(string title, List<DailyMetric> metrics)
    {
        if (metrics.Count == 0) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{title} (last {metrics.Count} days)[/]");

        var max = metrics.Max(m => m.Count);
        var min = metrics.Min(m => m.Count);
        var range = max - min;

        // Create a simple ASCII sparkline
        var sparkChars = new[] { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
        var spark = "";

        foreach (var metric in metrics)
        {
            var normalized = range > 0
                ? (metric.Count - min) / (double)range
                : 0.5;
            var index = (int)(normalized * (sparkChars.Length - 1));
            spark += sparkChars[index];
        }

        AnsiConsole.MarkupLine($"[green]{spark}[/]  [dim]min:{min} max:{max}[/]");
    }

    private static void DisplayWatchListAndAlerts(WatchListSummary watchList, AlertSummary alerts)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        // Watch list panel
        var watchTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Watch List[/]");

        watchTable.AddColumn("Metric");
        watchTable.AddColumn("Value");

        watchTable.AddRow("Total Watched", $"{watchList.TotalWatched:N0}");
        watchTable.AddRow("With Price Changes", watchList.WithPriceChanges > 0
            ? $"[yellow]{watchList.WithPriceChanges:N0}[/]"
            : "0");
        watchTable.AddRow("No Longer Active", watchList.NoLongerActive > 0
            ? $"[red]{watchList.NoLongerActive:N0}[/]"
            : "0");

        // Alert panel
        var alertTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Alerts[/]");

        alertTable.AddColumn("Metric");
        alertTable.AddColumn("Value");

        alertTable.AddRow("Total Alerts", $"{alerts.TotalAlerts:N0}");
        alertTable.AddRow("Active Alerts", $"[green]{alerts.ActiveAlerts:N0}[/]");
        alertTable.AddRow("Triggered Today", alerts.TriggeredToday > 0
            ? $"[yellow]{alerts.TriggeredToday:N0}[/]"
            : "0");
        alertTable.AddRow("Triggered This Week", $"{alerts.TriggeredThisWeek:N0}");

        grid.AddRow(watchTable, alertTable);
        AnsiConsole.Write(grid);

        // Recent changes from watch list
        if (watchList.RecentChanges.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Recent Watch List Changes[/]");

            var changesTable = new Table()
                .Border(TableBorder.Rounded)
                .HideHeaders();

            changesTable.AddColumn("Vehicle");
            changesTable.AddColumn("Change");
            changesTable.AddColumn("Time");

            foreach (var change in watchList.RecentChanges.Take(3))
            {
                var changeColor = change.ChangeType == "Price Drop" ? "green" : "red";
                changesTable.AddRow(
                    change.Title,
                    $"[{changeColor}]{change.ChangeType}[/]: {change.Details}",
                    GetRelativeTime(change.ChangedAt));
            }

            AnsiConsole.Write(changesTable);
        }

        // Recent alert notifications
        if (alerts.RecentNotifications.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Recent Alert Notifications[/]");

            var notifTable = new Table()
                .Border(TableBorder.Rounded)
                .HideHeaders();

            notifTable.AddColumn("Alert");
            notifTable.AddColumn("Listing");
            notifTable.AddColumn("Time");

            foreach (var notif in alerts.RecentNotifications.Take(3))
            {
                notifTable.AddRow(
                    $"[yellow]{notif.AlertName}[/]",
                    notif.ListingTitle,
                    GetRelativeTime(notif.TriggeredAt));
            }

            AnsiConsole.Write(notifTable);
        }
    }

    private static void DisplaySystemMetrics(SystemMetrics metrics)
    {
        AnsiConsole.MarkupLine("[bold]System Metrics[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .HideHeaders();

        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.AddColumn("Metric2");
        table.AddColumn("Value2");

        table.AddRow(
            "[dim]Total Sessions[/]",
            $"{metrics.TotalSearchSessions:N0}",
            "[dim]Last 24 Hours[/]",
            $"[blue]{metrics.SearchesLast24Hours:N0}[/]");

        table.AddRow(
            "[dim]Saved Profiles[/]",
            $"{metrics.SavedProfiles:N0}",
            "[dim]Price History[/]",
            $"{metrics.DatabaseStats.TotalPriceHistoryRecords:N0}");

        AnsiConsole.Write(table);
    }

    private void DisplayScraperHealth()
    {
        if (_healthService == null) return;

        AnsiConsole.MarkupLine("[bold]Scraper Health[/]");

        var allMetrics = _healthService.GetAllHealthMetrics();

        if (!allMetrics.Any())
        {
            AnsiConsole.MarkupLine("  [dim]No scraper activity recorded yet[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded);

        table.AddColumn("[bold]Site[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Success Rate[/]");
        table.AddColumn("[bold]Listings[/]");
        table.AddColumn("[bold]Last Run[/]");

        foreach (var (siteName, metrics) in allMetrics.OrderBy(kvp => kvp.Key).Take(6))
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

            table.AddRow(
                siteName,
                $"[{statusColor}]{status}[/]",
                $"[{successRateColor}]{metrics.SuccessRate:F0}%[/]",
                $"{metrics.ListingsFound:N0}",
                metrics.LastAttemptedAt != default
                    ? GetRelativeTime(metrics.LastAttemptedAt)
                    : "[dim]Never[/]");
        }

        AnsiConsole.Write(table);
    }

    private static string FormatTrend(TrendData trend, bool invertColor = false)
    {
        if (trend.PreviousValue == 0 && trend.CurrentValue == 0)
            return "[dim]—[/]";

        var arrow = trend.Direction switch
        {
            TrendDirection.Up => "↑",
            TrendDirection.Down => "↓",
            _ => "—"
        };

        var color = trend.Direction switch
        {
            TrendDirection.Up => invertColor ? "red" : "green",
            TrendDirection.Down => invertColor ? "green" : "red",
            _ => "dim"
        };

        var changeStr = trend.PercentageChange != 0
            ? $"{Math.Abs(trend.PercentageChange):F1}%"
            : "";

        return $"[{color}]{arrow} {changeStr}[/]";
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

        [CommandOption("-w|--watch")]
        [Description("Enable watch mode with auto-refresh")]
        [DefaultValue(false)]
        public bool Watch { get; set; }

        [CommandOption("-r|--refresh <SECONDS>")]
        [Description("Refresh interval in seconds for watch mode (default: 30)")]
        [DefaultValue(30)]
        public int RefreshInterval { get; set; } = 30;

        [CommandOption("-c|--compact")]
        [Description("Show compact dashboard view")]
        [DefaultValue(false)]
        public bool Compact { get; set; }

        [CommandOption("--trend-days <DAYS>")]
        [Description("Number of days for trend calculations (default: 30)")]
        [DefaultValue(30)]
        public int TrendDays { get; set; } = 30;

        [CommandOption("--show-health")]
        [Description("Include scraper health information")]
        [DefaultValue(true)]
        public bool ShowHealth { get; set; } = true;
    }
}
