using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to display market statistics and analysis.
/// </summary>
public class StatsCommand : AsyncCommand<StatsCommand.Settings>
{
    private readonly IStatisticsService _statisticsService;

    public StatsCommand(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Status()
                .Start("Calculating market statistics...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                });

            var statistics = await _statisticsService.GetMarketStatisticsAsync(
                settings.TenantId,
                settings.Make,
                settings.Model,
                settings.YearMin,
                settings.YearMax,
                settings.PriceMin,
                settings.PriceMax,
                settings.MileageMax,
                settings.Condition,
                settings.BodyStyle);

            if (statistics.TotalListings == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No listings found matching the specified criteria[/]");
                return ExitCodes.Success;
            }

            DisplayStatistics(statistics, settings);

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private static void DisplayStatistics(MarketStatistics stats, Settings settings)
    {
        // Title
        var title = "Market Statistics";
        if (!string.IsNullOrEmpty(settings.Make))
        {
            title += $" - {settings.Make}";
            if (!string.IsNullOrEmpty(settings.Model))
                title += $" {settings.Model}";
        }

        var panel = new Panel($"[bold]{title}[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Overview
        var overviewTable = new Table();
        overviewTable.Border(TableBorder.Rounded);
        overviewTable.AddColumn("[bold]Metric[/]");
        overviewTable.AddColumn("[bold]Value[/]");

        overviewTable.AddRow("Total Listings", $"[green]{stats.TotalListings:N0}[/]");

        if (stats.AveragePrice.HasValue)
            overviewTable.AddRow("Average Price", $"${stats.AveragePrice.Value:N2}");
        if (stats.MedianPrice.HasValue)
            overviewTable.AddRow("Median Price", $"${stats.MedianPrice.Value:N2}");
        if (stats.MinPrice.HasValue && stats.MaxPrice.HasValue)
            overviewTable.AddRow("Price Range", $"${stats.MinPrice.Value:N2} - ${stats.MaxPrice.Value:N2}");

        if (stats.AverageMileage.HasValue)
            overviewTable.AddRow("Average Mileage", $"{stats.AverageMileage.Value:N0} mi");
        if (stats.MedianMileage.HasValue)
            overviewTable.AddRow("Median Mileage", $"{stats.MedianMileage.Value:N0} mi");
        if (stats.MinMileage.HasValue && stats.MaxMileage.HasValue)
            overviewTable.AddRow("Mileage Range", $"{stats.MinMileage.Value:N0} - {stats.MaxMileage.Value:N0} mi");

        if (stats.AverageDaysOnMarket.HasValue)
            overviewTable.AddRow("Avg Days on Market", $"{stats.AverageDaysOnMarket.Value:F1} days");

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Count by attributes
        if (stats.CountByMake.Any())
        {
            AnsiConsole.MarkupLine("[bold]Distribution by Make:[/]");
            DisplayBarChart("Makes", stats.CountByMake.OrderByDescending(x => x.Value).Take(10));
            AnsiConsole.WriteLine();
        }

        if (stats.CountByModel.Any() && stats.CountByModel.Count <= 20)
        {
            AnsiConsole.MarkupLine("[bold]Distribution by Model:[/]");
            DisplayBarChart("Models", stats.CountByModel.OrderByDescending(x => x.Value).Take(10));
            AnsiConsole.WriteLine();
        }

        if (stats.CountByYear.Any())
        {
            AnsiConsole.MarkupLine("[bold]Distribution by Year:[/]");
            DisplayBarChart("Years", stats.CountByYear.OrderByDescending(x => x.Key).Take(10));
            AnsiConsole.WriteLine();
        }

        if (stats.CountByCondition.Any())
        {
            AnsiConsole.MarkupLine("[bold]Distribution by Condition:[/]");
            DisplayBarChart("Condition", stats.CountByCondition);
            AnsiConsole.WriteLine();
        }

        if (stats.CountByBodyStyle.Any())
        {
            AnsiConsole.MarkupLine("[bold]Distribution by Body Style:[/]");
            DisplayBarChart("Body Styles", stats.CountByBodyStyle.OrderByDescending(x => x.Value));
            AnsiConsole.WriteLine();
        }
    }

    private static void DisplayBarChart<T>(string title, IEnumerable<KeyValuePair<T, int>> data) where T : notnull
    {
        var chart = new BarChart()
            .Width(60)
            .Label($"[bold]{title}[/]");

        foreach (var item in data)
        {
            chart.AddItem(item.Key.ToString() ?? "Unknown", item.Value, Color.Green);
        }

        AnsiConsole.Write(chart);
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("-m|--make <MAKE>")]
        [Description("Filter by vehicle make")]
        public string? Make { get; set; }

        [CommandOption("--model <MODEL>")]
        [Description("Filter by vehicle model")]
        public string? Model { get; set; }

        [CommandOption("--year-min <YEAR>")]
        [Description("Minimum year")]
        public int? YearMin { get; set; }

        [CommandOption("--year-max <YEAR>")]
        [Description("Maximum year")]
        public int? YearMax { get; set; }

        [CommandOption("--price-min <PRICE>")]
        [Description("Minimum price")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max <PRICE>")]
        [Description("Maximum price")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--mileage-max <MILEAGE>")]
        [Description("Maximum mileage")]
        public int? MileageMax { get; set; }

        [CommandOption("--condition <CONDITION>")]
        [Description("Filter by condition (New, Used, CPO)")]
        public string? Condition { get; set; }

        [CommandOption("--body-style <STYLE>")]
        [Description("Filter by body style")]
        public string? BodyStyle { get; set; }
    }
}
