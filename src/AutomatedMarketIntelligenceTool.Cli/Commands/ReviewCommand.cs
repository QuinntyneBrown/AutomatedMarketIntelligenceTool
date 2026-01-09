using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage the review queue for near-match duplicates.
/// </summary>
public class ReviewCommand : AsyncCommand<ReviewCommand.Settings>
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewCommand> _logger;

    public ReviewCommand(
        IReviewService reviewService,
        ILogger<ReviewCommand> logger)
    {
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.Action.ToLowerInvariant() switch
            {
                "list" => await ListReviewsAsync(settings),
                "resolve" => await ResolveReviewAsync(settings),
                "dismiss" => await DismissReviewAsync(settings),
                "stats" => await ShowStatsAsync(settings),
                _ => await ListReviewsAsync(settings)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Review command failed: {ErrorMessage}", ex.Message);
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> ListReviewsAsync(Settings settings)
    {
        var options = new ReviewFilterOptions
        {
            Status = settings.Status,
            Page = settings.Page,
            PageSize = settings.PageSize
        };

        var result = await _reviewService.GetReviewsAsync(settings.TenantId, options);

        if (result.TotalCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No review items found.[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine($"[bold]Review Queue[/] - Page {result.Page} of {result.TotalPages} ({result.TotalCount} total)");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Status");
        table.AddColumn("Confidence");
        table.AddColumn("Method");
        table.AddColumn("Listing 1");
        table.AddColumn("Listing 2");
        table.AddColumn("Created");

        foreach (var item in result.Items)
        {
            var statusColor = item.Status switch
            {
                ReviewItemStatus.Pending => "yellow",
                ReviewItemStatus.Resolved => "green",
                ReviewItemStatus.Dismissed => "gray",
                _ => "white"
            };

            var methodStr = item.MatchMethod switch
            {
                MatchMethod.Fuzzy => "Fuzzy",
                MatchMethod.Image => "Image",
                MatchMethod.Combined => "Combined",
                _ => "Unknown"
            };

            table.AddRow(
                item.ReviewItemId.Value.ToString()[..8],
                $"[{statusColor}]{item.Status}[/]",
                $"{item.ConfidenceScore:F1}%",
                methodStr,
                $"{item.Listing1Year} {item.Listing1Make} {item.Listing1Model}\n[dim]{item.Listing1SourceSite}[/]",
                $"{item.Listing2Year} {item.Listing2Make} {item.Listing2Model}\n[dim]{item.Listing2SourceSite}[/]",
                item.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);

        if (result.TotalPages > 1)
        {
            AnsiConsole.MarkupLine($"\n[dim]Use --page to navigate. Example: --page {result.Page + 1}[/]");
        }

        return ExitCodes.Success;
    }

    private async Task<int> ResolveReviewAsync(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.ReviewId))
        {
            AnsiConsole.MarkupLine("[red]Error: Review ID is required for resolve action.[/]");
            return ExitCodes.ValidationError;
        }

        if (!Guid.TryParse(settings.ReviewId, out var reviewGuid))
        {
            // Try parsing as short ID
            var reviews = await _reviewService.GetPendingReviewsAsync(settings.TenantId);
            var match = reviews.FirstOrDefault(r =>
                r.ReviewItemId.Value.ToString().StartsWith(settings.ReviewId, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid review ID: {settings.ReviewId}[/]");
                return ExitCodes.ValidationError;
            }

            reviewGuid = match.ReviewItemId.Value;
        }

        var reviewItemId = new ReviewItemId(reviewGuid);

        // Get review details for display
        var review = await _reviewService.GetReviewByIdAsync(settings.TenantId, reviewItemId);
        if (review == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Review item not found.[/]");
            return ExitCodes.NotFound;
        }

        if (review.Status != ReviewItemStatus.Pending)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Review item is already {review.Status}.[/]");
            return ExitCodes.ValidationError;
        }

        // Display comparison
        DisplayComparison(review);

        // Determine decision
        ResolutionDecision decision;
        if (settings.SameVehicle)
        {
            decision = ResolutionDecision.SameVehicle;
        }
        else if (settings.DifferentVehicle)
        {
            decision = ResolutionDecision.DifferentVehicle;
        }
        else
        {
            // Interactive prompt
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Are these the [bold]same vehicle[/]?")
                    .AddChoices("Same Vehicle", "Different Vehicle", "Cancel"));

            decision = choice switch
            {
                "Same Vehicle" => ResolutionDecision.SameVehicle,
                "Different Vehicle" => ResolutionDecision.DifferentVehicle,
                _ => ResolutionDecision.None
            };

            if (decision == ResolutionDecision.None)
            {
                AnsiConsole.MarkupLine("[yellow]Resolution cancelled.[/]");
                return ExitCodes.Success;
            }
        }

        var success = await _reviewService.ResolveReviewAsync(
            settings.TenantId,
            reviewItemId,
            decision,
            Environment.UserName,
            settings.Notes);

        if (success)
        {
            var decisionStr = decision == ResolutionDecision.SameVehicle ? "same vehicle" : "different vehicles";
            AnsiConsole.MarkupLine($"[green]Review resolved as {decisionStr}.[/]");
            return ExitCodes.Success;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Failed to resolve review.[/]");
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> DismissReviewAsync(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.ReviewId))
        {
            AnsiConsole.MarkupLine("[red]Error: Review ID is required for dismiss action.[/]");
            return ExitCodes.ValidationError;
        }

        if (!Guid.TryParse(settings.ReviewId, out var reviewGuid))
        {
            // Try parsing as short ID
            var reviews = await _reviewService.GetPendingReviewsAsync(settings.TenantId);
            var match = reviews.FirstOrDefault(r =>
                r.ReviewItemId.Value.ToString().StartsWith(settings.ReviewId, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid review ID: {settings.ReviewId}[/]");
                return ExitCodes.ValidationError;
            }

            reviewGuid = match.ReviewItemId.Value;
        }

        var reviewItemId = new ReviewItemId(reviewGuid);

        var success = await _reviewService.DismissReviewAsync(
            settings.TenantId,
            reviewItemId,
            settings.Notes);

        if (success)
        {
            AnsiConsole.MarkupLine("[green]Review dismissed.[/]");
            return ExitCodes.Success;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Failed to dismiss review. It may already be resolved or not exist.[/]");
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> ShowStatsAsync(Settings settings)
    {
        var stats = await _reviewService.GetStatsAsync(settings.TenantId);

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Title("[bold]Review Queue Statistics[/]");
        table.AddColumn("Metric");
        table.AddColumn("Value");

        table.AddRow("Total Reviews", stats.TotalCount.ToString());
        table.AddRow("[yellow]Pending[/]", stats.PendingCount.ToString());
        table.AddRow("[green]Resolved[/]", stats.ResolvedCount.ToString());
        table.AddRow("[dim]Dismissed[/]", stats.DismissedCount.ToString());
        table.AddRow("", "");
        table.AddRow("Resolved as Same Vehicle", stats.SameVehicleCount.ToString());
        table.AddRow("Resolved as Different", stats.DifferentVehicleCount.ToString());
        table.AddRow("", "");
        table.AddRow("Avg. Confidence Score", $"{stats.AverageConfidenceScore:F1}%");

        AnsiConsole.Write(table);

        return ExitCodes.Success;
    }

    private static void DisplayComparison(ReviewItemInfo review)
    {
        AnsiConsole.MarkupLine($"\n[bold]Review Item[/] - {review.ConfidenceScore:F1}% confidence ({review.MatchMethod})");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Field[/]");
        table.AddColumn("[bold]Listing 1[/]");
        table.AddColumn("[bold]Listing 2[/]");
        table.AddColumn("[bold]Match[/]");

        // Vehicle info
        var makeMatch = review.Listing1Make == review.Listing2Make;
        var modelMatch = review.Listing1Model == review.Listing2Model;
        var yearMatch = review.Listing1Year == review.Listing2Year;

        table.AddRow(
            "Make/Model",
            $"{review.Listing1Make} {review.Listing1Model}",
            $"{review.Listing2Make} {review.Listing2Model}",
            makeMatch && modelMatch ? "[green]Yes[/]" : "[red]No[/]"
        );

        table.AddRow(
            "Year",
            review.Listing1Year.ToString(),
            review.Listing2Year.ToString(),
            yearMatch ? "[green]Yes[/]" : $"[yellow]Diff: {Math.Abs(review.Listing1Year - review.Listing2Year)}[/]"
        );

        table.AddRow(
            "Price",
            $"${review.Listing1Price:N0}",
            $"${review.Listing2Price:N0}",
            Math.Abs(review.Listing1Price - review.Listing2Price) <= 500 ? "[green]Close[/]" : $"[yellow]Diff: ${Math.Abs(review.Listing1Price - review.Listing2Price):N0}[/]"
        );

        var m1 = review.Listing1Mileage?.ToString("N0") ?? "N/A";
        var m2 = review.Listing2Mileage?.ToString("N0") ?? "N/A";
        var mileageDiff = review.Listing1Mileage.HasValue && review.Listing2Mileage.HasValue
            ? Math.Abs(review.Listing1Mileage.Value - review.Listing2Mileage.Value)
            : (int?)null;
        var mileageMatch = mileageDiff.HasValue && mileageDiff <= 500 ? "[green]Close[/]"
            : mileageDiff.HasValue ? $"[yellow]Diff: {mileageDiff:N0}[/]"
            : "[dim]N/A[/]";

        table.AddRow("Mileage", m1, m2, mileageMatch);

        table.AddRow(
            "Location",
            review.Listing1City ?? "N/A",
            review.Listing2City ?? "N/A",
            review.Listing1City == review.Listing2City ? "[green]Yes[/]" : "[yellow]Different[/]"
        );

        table.AddRow(
            "Source",
            review.Listing1SourceSite,
            review.Listing2SourceSite,
            review.Listing1SourceSite == review.Listing2SourceSite ? "[dim]Same[/]" : "[blue]Cross-site[/]"
        );

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action to perform: list, resolve, dismiss, stats")]
        [DefaultValue("list")]
        public string Action { get; set; } = "list";

        [CommandArgument(1, "[review-id]")]
        [Description("Review ID for resolve/dismiss actions (can be partial)")]
        public string? ReviewId { get; set; }

        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("--status")]
        [Description("Filter by status: Pending, Resolved, Dismissed")]
        public ReviewItemStatus? Status { get; set; }

        [CommandOption("--same-vehicle")]
        [Description("Resolve as same vehicle")]
        public bool SameVehicle { get; set; }

        [CommandOption("--different-vehicle")]
        [Description("Resolve as different vehicles")]
        public bool DifferentVehicle { get; set; }

        [CommandOption("--notes")]
        [Description("Notes for the resolution")]
        public string? Notes { get; set; }

        [CommandOption("--page")]
        [Description("Page number (default: 1)")]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        [CommandOption("--page-size")]
        [Description("Items per page (default: 20)")]
        [DefaultValue(20)]
        public int PageSize { get; set; } = 20;
    }
}
