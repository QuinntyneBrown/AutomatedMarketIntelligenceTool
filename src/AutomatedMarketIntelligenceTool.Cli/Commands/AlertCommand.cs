using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage alerts.
/// </summary>
public class AlertCommand : AsyncCommand<AlertCommand.Settings>
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertCommand> _logger;

    public AlertCommand(
        IAlertService alertService,
        ILogger<AlertCommand> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.Action.ToLowerInvariant() switch
            {
                "create" => await CreateAlertAsync(settings),
                "list" => await ListAlertsAsync(settings),
                "enable" => await EnableAlertAsync(settings),
                "disable" => await DisableAlertAsync(settings),
                "delete" => await DeleteAlertAsync(settings),
                _ => await ListAlertsAsync(settings)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert command failed: {ErrorMessage}", ex.Message);
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> CreateAlertAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: Alert name is required[/]");
            return ExitCodes.ValidationError;
        }

        var tenantId = settings.TenantId ?? Guid.Empty;

        var criteria = new AlertCriteria
        {
            Make = settings.Make,
            Model = settings.Model,
            YearMin = settings.YearMin,
            YearMax = settings.YearMax,
            PriceMin = settings.PriceMin,
            PriceMax = settings.PriceMax,
            MileageMax = settings.MileageMax,
            Location = settings.Location,
            Dealer = settings.Dealer
        };

        var alert = await _alertService.CreateAlertAsync(
            tenantId,
            settings.Name,
            criteria,
            NotificationMethod.Console);

        AnsiConsole.MarkupLine($"[green]✓ Created alert '{settings.Name}'[/]");
        AnsiConsole.MarkupLine($"  Alert ID: {alert.AlertId.Value}");
        DisplayAlertCriteria(criteria);

        return ExitCodes.Success;
    }

    private async Task<int> ListAlertsAsync(Settings settings)
    {
        var tenantId = settings.TenantId ?? Guid.Empty;
        var alerts = await _alertService.GetAllAlertsAsync(tenantId);

        if (!alerts.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No alerts configured[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Alert ID");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Triggers");
        table.AddColumn("Last Triggered");
        table.AddColumn("Criteria");

        foreach (var alert in alerts)
        {
            var status = alert.IsActive ? "[green]Active[/]" : "[dim]Inactive[/]";
            var lastTriggered = alert.LastTriggeredAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
            var criteria = GetCriteriaSummary(alert.GetCriteria());

            table.AddRow(
                alert.AlertId.Value.ToString()[..Math.Min(8, alert.AlertId.Value.ToString().Length)],
                alert.Name,
                status,
                alert.TriggerCount.ToString(),
                lastTriggered,
                criteria);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {alerts.Count} alerts[/]");

        return ExitCodes.Success;
    }

    private async Task<int> EnableAlertAsync(Settings settings)
    {
        if (settings.AlertId == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Alert ID is required[/]");
            return ExitCodes.ValidationError;
        }

        var tenantId = settings.TenantId ?? Guid.Empty;
        var alertId = new AlertId(settings.AlertId.Value);

        await _alertService.ActivateAlertAsync(tenantId, alertId);

        AnsiConsole.MarkupLine($"[green]✓ Enabled alert {settings.AlertId}[/]");
        return ExitCodes.Success;
    }

    private async Task<int> DisableAlertAsync(Settings settings)
    {
        if (settings.AlertId == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Alert ID is required[/]");
            return ExitCodes.ValidationError;
        }

        var tenantId = settings.TenantId ?? Guid.Empty;
        var alertId = new AlertId(settings.AlertId.Value);

        await _alertService.DeactivateAlertAsync(tenantId, alertId);

        AnsiConsole.MarkupLine($"[yellow]✓ Disabled alert {settings.AlertId}[/]");
        return ExitCodes.Success;
    }

    private async Task<int> DeleteAlertAsync(Settings settings)
    {
        if (settings.AlertId == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Alert ID is required[/]");
            return ExitCodes.ValidationError;
        }

        var tenantId = settings.TenantId ?? Guid.Empty;
        var alertId = new AlertId(settings.AlertId.Value);

        await _alertService.DeleteAlertAsync(tenantId, alertId);

        AnsiConsole.MarkupLine($"[red]✓ Deleted alert {settings.AlertId}[/]");
        return ExitCodes.Success;
    }

    private void DisplayAlertCriteria(AlertCriteria criteria)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(criteria.Make))
            parts.Add($"Make: {criteria.Make}");
        if (!string.IsNullOrEmpty(criteria.Model))
            parts.Add($"Model: {criteria.Model}");
        if (criteria.YearMin.HasValue)
            parts.Add($"Year ≥ {criteria.YearMin}");
        if (criteria.YearMax.HasValue)
            parts.Add($"Year ≤ {criteria.YearMax}");
        if (criteria.PriceMin.HasValue)
            parts.Add($"Price ≥ ${criteria.PriceMin:N2}");
        if (criteria.PriceMax.HasValue)
            parts.Add($"Price ≤ ${criteria.PriceMax:N2}");
        if (criteria.MileageMax.HasValue)
            parts.Add($"Mileage ≤ {criteria.MileageMax:N0}");
        if (!string.IsNullOrEmpty(criteria.Location))
            parts.Add($"Location: {criteria.Location}");
        if (!string.IsNullOrEmpty(criteria.Dealer))
            parts.Add($"Dealer: {criteria.Dealer}");

        if (parts.Any())
        {
            AnsiConsole.MarkupLine($"  Criteria: {string.Join(", ", parts)}");
        }
    }

    private string GetCriteriaSummary(AlertCriteria criteria)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(criteria.Make))
            parts.Add(criteria.Make);
        if (!string.IsNullOrEmpty(criteria.Model))
            parts.Add(criteria.Model);
        if (criteria.PriceMax.HasValue)
            parts.Add($"≤${criteria.PriceMax:N0}");

        return parts.Any() ? string.Join(" ", parts) : "Any";
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action to perform (create, list, enable, disable, delete)")]
        [DefaultValue("list")]
        public string Action { get; set; } = "list";

        [CommandOption("--name")]
        [Description("Alert name")]
        public string? Name { get; set; }

        [CommandOption("--alert-id")]
        [Description("Alert ID")]
        public Guid? AlertId { get; set; }

        [CommandOption("--make|-m")]
        [Description("Vehicle make to match")]
        public string? Make { get; set; }

        [CommandOption("--model")]
        [Description("Vehicle model to match")]
        public string? Model { get; set; }

        [CommandOption("--year-min")]
        [Description("Minimum year")]
        public int? YearMin { get; set; }

        [CommandOption("--year-max")]
        [Description("Maximum year")]
        public int? YearMax { get; set; }

        [CommandOption("--price-min")]
        [Description("Minimum price")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max")]
        [Description("Maximum price")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--mileage-max")]
        [Description("Maximum mileage")]
        public int? MileageMax { get; set; }

        [CommandOption("--location|-l")]
        [Description("Location to match")]
        public string? Location { get; set; }

        [CommandOption("--dealer")]
        [Description("Dealer name to match")]
        public string? Dealer { get; set; }

        [CommandOption("--tenant-id")]
        [Description("Tenant ID (defaults to Guid.Empty for single-tenant mode)")]
        public Guid? TenantId { get; set; }
    }
}
