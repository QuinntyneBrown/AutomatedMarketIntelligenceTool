using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Deduplication;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to view and manage deduplication audit trail.
/// </summary>
public class AuditCommand : AsyncCommand<AuditCommand.Settings>
{
    private readonly IDeduplicationAuditService _auditService;
    private readonly IAccuracyMetricsService _metricsService;

    public AuditCommand(
        IDeduplicationAuditService auditService,
        IAccuracyMetricsService metricsService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.SubCommand?.ToLowerInvariant() switch
            {
                "metrics" or "accuracy" => await ShowMetricsAsync(settings),
                "false-positives" or "fp" => await ShowFalsePositivesAsync(settings),
                "mark-fp" => await MarkAsFalsePositiveAsync(settings),
                "mark-fn" => await MarkAsFalseNegativeAsync(settings),
                "clear" => await ClearErrorFlagsAsync(settings),
                _ => await ListAuditEntriesAsync(settings)
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> ListAuditEntriesAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Loading audit entries...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        var filter = BuildFilter(settings);
        var result = await _auditService.QueryAuditEntriesAsync(settings.TenantId, filter);

        if (result.TotalCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No audit entries found matching the specified criteria[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]ID[/]");
        table.AddColumn("[bold]Date[/]");
        table.AddColumn("[bold]Decision[/]");
        table.AddColumn("[bold]Reason[/]");
        table.AddColumn("[bold]Confidence[/]");
        table.AddColumn("[bold]Auto[/]");
        table.AddColumn("[bold]FP/FN[/]");

        foreach (var entry in result.Items)
        {
            var decisionColor = entry.Decision switch
            {
                AuditDecision.Duplicate => "green",
                AuditDecision.NewListing => "blue",
                AuditDecision.NearMatch => "yellow",
                AuditDecision.ManualOverride => "cyan",
                _ => "white"
            };

            var fpFnMarker = entry.IsFalsePositive ? "[red]FP[/]" :
                             entry.IsFalseNegative ? "[yellow]FN[/]" : "-";

            table.AddRow(
                entry.AuditEntryId.Value.ToString()[..8],
                entry.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                $"[{decisionColor}]{entry.Decision}[/]",
                entry.Reason.ToString(),
                entry.ConfidenceScore.HasValue ? $"{entry.ConfidenceScore.Value:F1}%" : "-",
                entry.WasAutomatic ? "[green]Yes[/]" : "[cyan]No[/]",
                fpFnMarker
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\nShowing {result.Items.Count} of {result.TotalCount} entries");

        if (result.HasMore)
        {
            AnsiConsole.MarkupLine("[dim]Use --skip and --take to paginate through results[/]");
        }

        return ExitCodes.Success;
    }

    private async Task<int> ShowMetricsAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Calculating accuracy metrics...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        var metrics = await _metricsService.CalculateMetricsAsync(
            settings.TenantId,
            settings.FromDate,
            settings.ToDate);

        if (metrics.TotalDecisions == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No audit data available for metrics calculation[/]");
            return ExitCodes.Success;
        }

        // Display header
        var panel = new Panel("[bold]Deduplication Accuracy Metrics[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Confusion Matrix
        AnsiConsole.MarkupLine("[bold]Confusion Matrix:[/]");
        var confusionTable = new Table();
        confusionTable.Border(TableBorder.Rounded);
        confusionTable.AddColumn("");
        confusionTable.AddColumn("[bold]Predicted Duplicate[/]");
        confusionTable.AddColumn("[bold]Predicted New[/]");
        confusionTable.AddRow(
            "[bold]Actual Duplicate[/]",
            $"[green]TP: {metrics.TruePositives}[/]",
            $"[red]FN: {metrics.FalseNegatives}[/]");
        confusionTable.AddRow(
            "[bold]Actual New[/]",
            $"[red]FP: {metrics.FalsePositives}[/]",
            $"[green]TN: {metrics.TrueNegatives}[/]");
        AnsiConsole.Write(confusionTable);
        AnsiConsole.WriteLine();

        // Metrics Table
        AnsiConsole.MarkupLine("[bold]Performance Metrics:[/]");
        var metricsTable = new Table();
        metricsTable.Border(TableBorder.Rounded);
        metricsTable.AddColumn("[bold]Metric[/]");
        metricsTable.AddColumn("[bold]Value[/]");
        metricsTable.AddColumn("[bold]Description[/]");

        metricsTable.AddRow("Total Decisions", $"[green]{metrics.TotalDecisions:N0}[/]", "Total audit entries");
        metricsTable.AddRow("Precision", FormatPercent(metrics.Precision), "TP / (TP + FP) - How many identified dupes are actual dupes");
        metricsTable.AddRow("Recall", FormatPercent(metrics.Recall), "TP / (TP + FN) - How many actual dupes were found");
        metricsTable.AddRow("F1 Score", FormatPercent(metrics.F1Score), "Harmonic mean of precision and recall");
        metricsTable.AddRow("Accuracy", FormatPercent(metrics.Accuracy), "(TP + TN) / Total");
        metricsTable.AddRow("Specificity", FormatPercent(metrics.Specificity), "TN / (TN + FP) - True negative rate");
        metricsTable.AddRow("FP Rate", FormatPercent(metrics.FalsePositiveRate, isNegative: true), "FP / (FP + TN)");
        metricsTable.AddRow("FN Rate", FormatPercent(metrics.FalseNegativeRate, isNegative: true), "FN / (FN + TP)");

        AnsiConsole.Write(metricsTable);

        // Show metrics by reason if verbose
        if (settings.Verbose)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Metrics by Matching Method:[/]");

            var metricsByReason = await _metricsService.GetMetricsByReasonAsync(
                settings.TenantId,
                settings.FromDate,
                settings.ToDate);

            var reasonTable = new Table();
            reasonTable.Border(TableBorder.Rounded);
            reasonTable.AddColumn("[bold]Method[/]");
            reasonTable.AddColumn("[bold]Count[/]");
            reasonTable.AddColumn("[bold]Precision[/]");
            reasonTable.AddColumn("[bold]Recall[/]");
            reasonTable.AddColumn("[bold]F1[/]");

            foreach (var (reason, m) in metricsByReason.OrderByDescending(x => x.Value.TotalDecisions))
            {
                reasonTable.AddRow(
                    reason.ToString(),
                    m.TotalDecisions.ToString(),
                    FormatPercent(m.Precision),
                    FormatPercent(m.Recall),
                    FormatPercent(m.F1Score)
                );
            }

            AnsiConsole.Write(reasonTable);
        }

        return ExitCodes.Success;
    }

    private async Task<int> ShowFalsePositivesAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Loading false positive statistics...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        var stats = await _auditService.GetFalsePositiveStatsAsync(
            settings.TenantId,
            settings.FromDate,
            settings.ToDate);

        if (stats.TotalDecisions == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No audit data available[/]");
            return ExitCodes.Success;
        }

        var panel = new Panel("[bold]False Positive/Negative Tracking[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Count[/]");
        table.AddColumn("[bold]Rate[/]");

        table.AddRow("Total Decisions", stats.TotalDecisions.ToString(), "-");
        table.AddRow("[red]False Positives[/]", stats.FalsePositiveCount.ToString(), FormatPercent(stats.FalsePositiveRate, isNegative: true));
        table.AddRow("[yellow]False Negatives[/]", stats.FalseNegativeCount.ToString(), FormatPercent(stats.FalseNegativeRate, isNegative: true));
        table.AddRow("[green]True Positives[/]", stats.TruePositiveCount.ToString(), "-");
        table.AddRow("[green]True Negatives[/]", stats.TrueNegativeCount.ToString(), "-");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Decision breakdown
        if (stats.DecisionBreakdown.Any())
        {
            AnsiConsole.MarkupLine("[bold]Decisions by Type:[/]");
            var chart = new BarChart().Width(60);
            foreach (var (decision, count) in stats.DecisionBreakdown.OrderByDescending(x => x.Value))
            {
                chart.AddItem(decision.ToString(), count, Color.Green);
            }
            AnsiConsole.Write(chart);
            AnsiConsole.WriteLine();
        }

        // Reason breakdown
        if (stats.ReasonBreakdown.Any())
        {
            AnsiConsole.MarkupLine("[bold]Decisions by Method:[/]");
            var chart = new BarChart().Width(60);
            foreach (var (reason, count) in stats.ReasonBreakdown.OrderByDescending(x => x.Value))
            {
                chart.AddItem(reason.ToString(), count, Color.Blue);
            }
            AnsiConsole.Write(chart);
        }

        return ExitCodes.Success;
    }

    private async Task<int> MarkAsFalsePositiveAsync(Settings settings)
    {
        if (!settings.AuditEntryId.HasValue)
        {
            AnsiConsole.MarkupLine("[red]Error: --entry-id is required[/]");
            return ExitCodes.ValidationError;
        }

        var success = await _auditService.MarkAsFalsePositiveAsync(
            settings.TenantId,
            settings.AuditEntryId.Value);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Marked entry {settings.AuditEntryId.Value} as false positive[/]");
            return ExitCodes.Success;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Entry not found[/]");
            return ExitCodes.NotFound;
        }
    }

    private async Task<int> MarkAsFalseNegativeAsync(Settings settings)
    {
        if (!settings.AuditEntryId.HasValue)
        {
            AnsiConsole.MarkupLine("[red]Error: --entry-id is required[/]");
            return ExitCodes.ValidationError;
        }

        var success = await _auditService.MarkAsFalseNegativeAsync(
            settings.TenantId,
            settings.AuditEntryId.Value);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Marked entry {settings.AuditEntryId.Value} as false negative[/]");
            return ExitCodes.Success;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Entry not found[/]");
            return ExitCodes.NotFound;
        }
    }

    private async Task<int> ClearErrorFlagsAsync(Settings settings)
    {
        if (!settings.AuditEntryId.HasValue)
        {
            AnsiConsole.MarkupLine("[red]Error: --entry-id is required[/]");
            return ExitCodes.ValidationError;
        }

        var success = await _auditService.ClearErrorFlagsAsync(
            settings.TenantId,
            settings.AuditEntryId.Value);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Cleared error flags from entry {settings.AuditEntryId.Value}[/]");
            return ExitCodes.Success;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Entry not found[/]");
            return ExitCodes.NotFound;
        }
    }

    private static AuditQueryFilter BuildFilter(Settings settings)
    {
        return new AuditQueryFilter
        {
            Decision = settings.Decision,
            FromDate = settings.FromDate,
            ToDate = settings.ToDate,
            WasAutomatic = settings.AutoOnly ? true : settings.ManualOnly ? false : null,
            IsFalsePositive = settings.FalsePositivesOnly ? true : null,
            IsFalseNegative = settings.FalseNegativesOnly ? true : null,
            Skip = settings.Skip,
            Take = settings.Take,
            SortDescending = !settings.Ascending
        };
    }

    private static string FormatPercent(double value, bool isNegative = false)
    {
        var percent = value * 100;
        var color = isNegative
            ? (percent > 5 ? "red" : percent > 1 ? "yellow" : "green")
            : (percent >= 95 ? "green" : percent >= 80 ? "yellow" : "red");
        return $"[{color}]{percent:F1}%[/]";
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[subcommand]")]
        [Description("Subcommand: list (default), metrics, false-positives, mark-fp, mark-fn, clear")]
        public string? SubCommand { get; set; }

        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("--decision <DECISION>")]
        [Description("Filter by decision type (NewListing, Duplicate, NearMatch, ManualOverride)")]
        public AuditDecision? Decision { get; set; }

        [CommandOption("--from <DATE>")]
        [Description("Filter by start date (YYYY-MM-DD)")]
        public DateTime? FromDate { get; set; }

        [CommandOption("--to <DATE>")]
        [Description("Filter by end date (YYYY-MM-DD)")]
        public DateTime? ToDate { get; set; }

        [CommandOption("--auto-only")]
        [Description("Show only automatic decisions")]
        public bool AutoOnly { get; set; }

        [CommandOption("--manual-only")]
        [Description("Show only manual decisions")]
        public bool ManualOnly { get; set; }

        [CommandOption("--fp-only|--false-positives-only")]
        [Description("Show only entries marked as false positives")]
        public bool FalsePositivesOnly { get; set; }

        [CommandOption("--fn-only|--false-negatives-only")]
        [Description("Show only entries marked as false negatives")]
        public bool FalseNegativesOnly { get; set; }

        [CommandOption("--entry-id <GUID>")]
        [Description("Audit entry ID for mark-fp/mark-fn/clear commands")]
        public Guid? AuditEntryId { get; set; }

        [CommandOption("--skip <COUNT>")]
        [Description("Number of entries to skip (pagination)")]
        [DefaultValue(0)]
        public int Skip { get; set; }

        [CommandOption("--take <COUNT>")]
        [Description("Number of entries to return (pagination)")]
        [DefaultValue(50)]
        public int Take { get; set; }

        [CommandOption("--ascending")]
        [Description("Sort in ascending order (default is descending by date)")]
        public bool Ascending { get; set; }

        [CommandOption("-v|--verbose")]
        [Description("Show detailed output")]
        public bool Verbose { get; set; }
    }
}
