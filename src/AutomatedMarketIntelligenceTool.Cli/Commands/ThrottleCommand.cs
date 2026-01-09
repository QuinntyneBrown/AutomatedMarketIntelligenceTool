using System.ComponentModel;
using System.Text;
using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Throttling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage resource throttling.
/// </summary>
public class ThrottleCommand : AsyncCommand<ThrottleCommand.Settings>
{
    private readonly IResourceThrottleService _throttleService;

    public ThrottleCommand(IResourceThrottleService throttleService)
    {
        _throttleService = throttleService ?? throw new ArgumentNullException(nameof(throttleService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.SubCommand?.ToLowerInvariant() switch
            {
                "create" or "add" => await CreateThrottleAsync(settings),
                "update" or "edit" => await UpdateThrottleAsync(settings),
                "delete" or "remove" => await DeleteThrottleAsync(settings),
                "enable" => await EnableThrottleAsync(settings),
                "disable" => await DisableThrottleAsync(settings),
                "reset" => await ResetUsageAsync(settings),
                "status" => await ShowStatusAsync(settings),
                "init" => await InitializeDefaultsAsync(settings),
                _ => await ListThrottlesAsync(settings)
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> ListThrottlesAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Loading resource throttles...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        var throttles = await _throttleService.GetAllThrottlesAsync(settings.TenantId, settings.EnabledOnly);

        if (throttles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No resource throttles configured[/]");
            AnsiConsole.MarkupLine("[dim]Run 'throttle init' to initialize default throttles[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Resource[/]");
        table.AddColumn("[bold]Limit[/]");
        table.AddColumn("[bold]Window[/]");
        table.AddColumn("[bold]Usage[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Action[/]");

        foreach (var throttle in throttles)
        {
            throttle.CheckAndResetWindow();
            var usagePercent = throttle.GetUsagePercent();
            var usageColor = usagePercent >= 100 ? "red" :
                            usagePercent >= throttle.WarningThresholdPercent ? "yellow" : "green";

            table.AddRow(
                throttle.ResourceType.ToString(),
                throttle.MaxValue.ToString(),
                throttle.TimeWindow.ToString(),
                $"[{usageColor}]{throttle.CurrentUsage}/{throttle.MaxValue} ({usagePercent:F0}%)[/]",
                throttle.IsEnabled ? "[green]Enabled[/]" : "[dim]Disabled[/]",
                throttle.Action.ToString()
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {throttles.Count} throttle(s)[/]");

        return ExitCodes.Success;
    }

    private async Task<int> ShowStatusAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Loading resource status...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        var statuses = await _throttleService.GetStatusAsync(settings.TenantId);

        if (statuses.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No resource throttles configured[/]");
            return ExitCodes.Success;
        }

        var panel = new Panel("[bold]Resource Throttling Status[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        foreach (var status in statuses.Where(s => s.IsEnabled))
        {
            var usageColor = status.IsLimitReached ? "red" :
                            status.IsAtWarningThreshold ? "yellow" : "green";

            var progressBar = new StringBuilder();
            var filled = (int)(status.UsagePercent / 5);
            for (int i = 0; i < 20; i++)
            {
                progressBar.Append(i < filled ? "█" : "░");
            }

            AnsiConsole.MarkupLine($"[bold]{status.ResourceType}[/]");
            AnsiConsole.MarkupLine($"  [{usageColor}]{progressBar}[/] {status.CurrentUsage}/{status.MaxValue} ({status.UsagePercent:F0}%)");
            AnsiConsole.MarkupLine($"  [dim]Remaining: {status.RemainingCapacity} | Resets in: {status.TimeUntilReset:hh\\:mm\\:ss}[/]");
            AnsiConsole.WriteLine();
        }

        return ExitCodes.Success;
    }

    private async Task<int> CreateThrottleAsync(Settings settings)
    {
        if (!Enum.TryParse<ResourceType>(settings.ResourceType, true, out var resourceType))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid resource type[/]");
            ShowResourceTypes();
            return ExitCodes.ValidationError;
        }

        if (!settings.MaxValue.HasValue)
        {
            AnsiConsole.MarkupLine("[red]Error: --max-value is required[/]");
            return ExitCodes.ValidationError;
        }

        if (!Enum.TryParse<ThrottleTimeWindow>(settings.TimeWindow ?? "PerMinute", true, out var timeWindow))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid time window[/]");
            return ExitCodes.ValidationError;
        }

        if (!Enum.TryParse<ThrottleAction>(settings.Action ?? "Reject", true, out var action))
        {
            action = ThrottleAction.Reject;
        }

        var throttle = await _throttleService.CreateThrottleAsync(
            settings.TenantId,
            resourceType,
            settings.MaxValue.Value,
            timeWindow,
            action,
            settings.WarningThreshold,
            settings.Description
        );

        AnsiConsole.MarkupLine($"[green]Created throttle for {throttle.ResourceType} with max {throttle.MaxValue} {throttle.TimeWindow}[/]");
        return ExitCodes.Success;
    }

    private async Task<int> UpdateThrottleAsync(Settings settings)
    {
        if (!Enum.TryParse<ResourceType>(settings.ResourceType, true, out var resourceType))
        {
            AnsiConsole.MarkupLine("[red]Error: --resource-type is required[/]");
            ShowResourceTypes();
            return ExitCodes.ValidationError;
        }

        var throttle = await _throttleService.GetThrottleByTypeAsync(settings.TenantId, resourceType);
        if (throttle == null)
        {
            AnsiConsole.MarkupLine($"[red]Throttle for {resourceType} not found[/]");
            return ExitCodes.NotFound;
        }

        if (!Enum.TryParse<ThrottleTimeWindow>(settings.TimeWindow ?? throttle.TimeWindow.ToString(), true, out var timeWindow))
            timeWindow = throttle.TimeWindow;

        if (!Enum.TryParse<ThrottleAction>(settings.Action ?? throttle.Action.ToString(), true, out var action))
            action = throttle.Action;

        var updated = await _throttleService.UpdateThrottleAsync(
            settings.TenantId,
            throttle.ResourceThrottleId,
            settings.MaxValue ?? throttle.MaxValue,
            timeWindow,
            action,
            settings.WarningThreshold > 0 ? settings.WarningThreshold : throttle.WarningThresholdPercent,
            settings.Description ?? throttle.Description
        );

        AnsiConsole.MarkupLine($"[green]Updated throttle for {updated.ResourceType}[/]");
        return ExitCodes.Success;
    }

    private async Task<int> DeleteThrottleAsync(Settings settings)
    {
        if (!Enum.TryParse<ResourceType>(settings.ResourceType, true, out var resourceType))
        {
            AnsiConsole.MarkupLine("[red]Error: --resource-type is required[/]");
            return ExitCodes.ValidationError;
        }

        var throttle = await _throttleService.GetThrottleByTypeAsync(settings.TenantId, resourceType);
        if (throttle == null)
        {
            AnsiConsole.MarkupLine($"[red]Throttle for {resourceType} not found[/]");
            return ExitCodes.NotFound;
        }

        if (!settings.Force)
        {
            if (!AnsiConsole.Confirm($"Delete throttle for {resourceType}?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                return ExitCodes.Success;
            }
        }

        var success = await _throttleService.DeleteThrottleAsync(settings.TenantId, throttle.ResourceThrottleId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Deleted throttle for {resourceType}[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to delete throttle[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> EnableThrottleAsync(Settings settings)
    {
        if (!Enum.TryParse<ResourceType>(settings.ResourceType, true, out var resourceType))
        {
            AnsiConsole.MarkupLine("[red]Error: --resource-type is required[/]");
            return ExitCodes.ValidationError;
        }

        var throttle = await _throttleService.GetThrottleByTypeAsync(settings.TenantId, resourceType);
        if (throttle == null)
        {
            AnsiConsole.MarkupLine($"[red]Throttle for {resourceType} not found[/]");
            return ExitCodes.NotFound;
        }

        var success = await _throttleService.EnableThrottleAsync(settings.TenantId, throttle.ResourceThrottleId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Enabled throttle for {resourceType}[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to enable throttle[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> DisableThrottleAsync(Settings settings)
    {
        if (!Enum.TryParse<ResourceType>(settings.ResourceType, true, out var resourceType))
        {
            AnsiConsole.MarkupLine("[red]Error: --resource-type is required[/]");
            return ExitCodes.ValidationError;
        }

        var throttle = await _throttleService.GetThrottleByTypeAsync(settings.TenantId, resourceType);
        if (throttle == null)
        {
            AnsiConsole.MarkupLine($"[red]Throttle for {resourceType} not found[/]");
            return ExitCodes.NotFound;
        }

        var success = await _throttleService.DisableThrottleAsync(settings.TenantId, throttle.ResourceThrottleId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[yellow]Disabled throttle for {resourceType}[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to disable throttle[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> ResetUsageAsync(Settings settings)
    {
        if (!Enum.TryParse<ResourceType>(settings.ResourceType, true, out var resourceType))
        {
            AnsiConsole.MarkupLine("[red]Error: --resource-type is required[/]");
            return ExitCodes.ValidationError;
        }

        var throttle = await _throttleService.GetThrottleByTypeAsync(settings.TenantId, resourceType);
        if (throttle == null)
        {
            AnsiConsole.MarkupLine($"[red]Throttle for {resourceType} not found[/]");
            return ExitCodes.NotFound;
        }

        var success = await _throttleService.ResetUsageAsync(settings.TenantId, throttle.ResourceThrottleId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Reset usage for {resourceType}[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to reset usage[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> InitializeDefaultsAsync(Settings settings)
    {
        await _throttleService.InitializeDefaultThrottlesAsync(settings.TenantId);
        AnsiConsole.MarkupLine("[green]Initialized default resource throttles[/]");
        return ExitCodes.Success;
    }

    private static void ShowResourceTypes()
    {
        AnsiConsole.MarkupLine("[dim]Available resource types:[/]");
        foreach (var type in Enum.GetNames<ResourceType>())
        {
            AnsiConsole.MarkupLine($"  - {type}");
        }
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[subcommand]")]
        [Description("Subcommand: list (default), create, update, delete, enable, disable, reset, status, init")]
        public string? SubCommand { get; set; }

        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("-r|--resource-type <TYPE>")]
        [Description("Resource type (ApiRequests, ConcurrentScrapers, ReportGenerations, etc.)")]
        public string? ResourceType { get; set; }

        [CommandOption("-m|--max-value <VALUE>")]
        [Description("Maximum allowed value")]
        public int? MaxValue { get; set; }

        [CommandOption("-w|--time-window <WINDOW>")]
        [Description("Time window (PerSecond, PerMinute, PerHour, PerDay, Concurrent)")]
        public string? TimeWindow { get; set; }

        [CommandOption("-a|--action <ACTION>")]
        [Description("Action when limit reached (Reject, Queue, Delay, LogOnly)")]
        public string? Action { get; set; }

        [CommandOption("--warning <PERCENT>")]
        [Description("Warning threshold percentage (0-100)")]
        [DefaultValue(80)]
        public int WarningThreshold { get; set; } = 80;

        [CommandOption("-d|--description <TEXT>")]
        [Description("Throttle description")]
        public string? Description { get; set; }

        [CommandOption("--enabled-only")]
        [Description("Show only enabled throttles")]
        public bool EnabledOnly { get; set; }

        [CommandOption("-f|--force")]
        [Description("Skip confirmation prompts")]
        public bool Force { get; set; }
    }
}
