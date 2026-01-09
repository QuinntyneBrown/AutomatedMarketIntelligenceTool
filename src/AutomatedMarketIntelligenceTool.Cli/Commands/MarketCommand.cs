using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Services.CustomMarkets;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage custom market regions.
/// </summary>
public class MarketCommand : AsyncCommand<MarketCommand.Settings>
{
    private readonly ICustomMarketService _marketService;

    public MarketCommand(ICustomMarketService marketService)
    {
        _marketService = marketService ?? throw new ArgumentNullException(nameof(marketService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.SubCommand?.ToLowerInvariant() switch
            {
                "create" or "add" => await CreateMarketAsync(settings),
                "update" or "edit" => await UpdateMarketAsync(settings),
                "delete" or "remove" => await DeleteMarketAsync(settings),
                "activate" => await ActivateMarketAsync(settings),
                "deactivate" => await DeactivateMarketAsync(settings),
                "show" or "get" => await ShowMarketAsync(settings),
                _ => await ListMarketsAsync(settings)
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> ListMarketsAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Loading custom markets...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        var markets = await _marketService.GetAllMarketsAsync(settings.TenantId, settings.ActiveOnly);

        if (markets.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No custom markets found[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Postal Codes[/]");
        table.AddColumn("[bold]Provinces[/]");
        table.AddColumn("[bold]Radius[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Priority[/]");

        foreach (var market in markets)
        {
            var postalCodes = market.PostalCodes.Length > 30
                ? market.PostalCodes[..27] + "..."
                : market.PostalCodes;

            table.AddRow(
                market.Name,
                postalCodes,
                market.Provinces ?? "-",
                market.RadiusKm.HasValue ? $"{market.RadiusKm}km" : "-",
                market.IsActive ? "[green]Active[/]" : "[yellow]Inactive[/]",
                market.Priority.ToString()
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {markets.Count} market(s)[/]");

        return ExitCodes.Success;
    }

    private async Task<int> CreateMarketAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required[/]");
            return ExitCodes.ValidationError;
        }

        if (string.IsNullOrWhiteSpace(settings.PostalCodes))
        {
            AnsiConsole.MarkupLine("[red]Error: --postal-codes is required[/]");
            return ExitCodes.ValidationError;
        }

        var market = await _marketService.CreateMarketAsync(
            settings.TenantId,
            settings.Name,
            settings.PostalCodes,
            settings.Description,
            settings.Provinces,
            settings.Latitude,
            settings.Longitude,
            settings.Radius,
            settings.Priority
        );

        AnsiConsole.MarkupLine($"[green]Created custom market '{market.Name}' ({market.CustomMarketId.Value})[/]");
        return ExitCodes.Success;
    }

    private async Task<int> UpdateMarketAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required to identify the market[/]");
            return ExitCodes.ValidationError;
        }

        var market = await _marketService.GetMarketByNameAsync(settings.TenantId, settings.Name);
        if (market == null)
        {
            AnsiConsole.MarkupLine($"[red]Market '{settings.Name}' not found[/]");
            return ExitCodes.NotFound;
        }

        var updated = await _marketService.UpdateMarketAsync(
            settings.TenantId,
            market.CustomMarketId,
            settings.NewName ?? market.Name,
            settings.PostalCodes ?? market.PostalCodes,
            settings.Description ?? market.Description,
            settings.Provinces ?? market.Provinces,
            settings.Latitude ?? market.CenterLatitude,
            settings.Longitude ?? market.CenterLongitude,
            settings.Radius ?? market.RadiusKm,
            settings.Priority > 0 ? settings.Priority : market.Priority
        );

        AnsiConsole.MarkupLine($"[green]Updated custom market '{updated.Name}'[/]");
        return ExitCodes.Success;
    }

    private async Task<int> DeleteMarketAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required[/]");
            return ExitCodes.ValidationError;
        }

        var market = await _marketService.GetMarketByNameAsync(settings.TenantId, settings.Name);
        if (market == null)
        {
            AnsiConsole.MarkupLine($"[red]Market '{settings.Name}' not found[/]");
            return ExitCodes.NotFound;
        }

        if (!settings.Force)
        {
            if (!AnsiConsole.Confirm($"Delete market '{settings.Name}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                return ExitCodes.Success;
            }
        }

        var success = await _marketService.DeleteMarketAsync(settings.TenantId, market.CustomMarketId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Deleted custom market '{settings.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to delete market[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> ActivateMarketAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required[/]");
            return ExitCodes.ValidationError;
        }

        var market = await _marketService.GetMarketByNameAsync(settings.TenantId, settings.Name);
        if (market == null)
        {
            AnsiConsole.MarkupLine($"[red]Market '{settings.Name}' not found[/]");
            return ExitCodes.NotFound;
        }

        var success = await _marketService.ActivateMarketAsync(settings.TenantId, market.CustomMarketId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Activated custom market '{settings.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to activate market[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> DeactivateMarketAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required[/]");
            return ExitCodes.ValidationError;
        }

        var market = await _marketService.GetMarketByNameAsync(settings.TenantId, settings.Name);
        if (market == null)
        {
            AnsiConsole.MarkupLine($"[red]Market '{settings.Name}' not found[/]");
            return ExitCodes.NotFound;
        }

        var success = await _marketService.DeactivateMarketAsync(settings.TenantId, market.CustomMarketId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[yellow]Deactivated custom market '{settings.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to deactivate market[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> ShowMarketAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required[/]");
            return ExitCodes.ValidationError;
        }

        var market = await _marketService.GetMarketByNameAsync(settings.TenantId, settings.Name);
        if (market == null)
        {
            AnsiConsole.MarkupLine($"[red]Market '{settings.Name}' not found[/]");
            return ExitCodes.NotFound;
        }

        var panel = new Panel($"[bold]Custom Market: {market.Name}[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Property[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("ID", market.CustomMarketId.Value.ToString());
        table.AddRow("Name", market.Name);
        table.AddRow("Description", market.Description ?? "-");
        table.AddRow("Postal Codes", market.PostalCodes);
        table.AddRow("Provinces", market.Provinces ?? "-");
        table.AddRow("Center", market.CenterLatitude.HasValue && market.CenterLongitude.HasValue
            ? $"{market.CenterLatitude:F4}, {market.CenterLongitude:F4}" : "-");
        table.AddRow("Radius", market.RadiusKm.HasValue ? $"{market.RadiusKm}km" : "-");
        table.AddRow("Status", market.IsActive ? "[green]Active[/]" : "[yellow]Inactive[/]");
        table.AddRow("Priority", market.Priority.ToString());
        table.AddRow("Created", market.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Updated", market.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        AnsiConsole.Write(table);
        return ExitCodes.Success;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[subcommand]")]
        [Description("Subcommand: list (default), create, update, delete, activate, deactivate, show")]
        public string? SubCommand { get; set; }

        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("-n|--name <NAME>")]
        [Description("Market name")]
        public string? Name { get; set; }

        [CommandOption("--new-name <NAME>")]
        [Description("New name for update command")]
        public string? NewName { get; set; }

        [CommandOption("-d|--description <TEXT>")]
        [Description("Market description")]
        public string? Description { get; set; }

        [CommandOption("-p|--postal-codes <CODES>")]
        [Description("Comma-separated postal codes")]
        public string? PostalCodes { get; set; }

        [CommandOption("--provinces <PROVINCES>")]
        [Description("Comma-separated provinces/states")]
        public string? Provinces { get; set; }

        [CommandOption("--lat|--latitude <LAT>")]
        [Description("Center latitude")]
        public double? Latitude { get; set; }

        [CommandOption("--lon|--longitude <LON>")]
        [Description("Center longitude")]
        public double? Longitude { get; set; }

        [CommandOption("-r|--radius <KM>")]
        [Description("Radius in kilometers")]
        public int? Radius { get; set; }

        [CommandOption("--priority <NUM>")]
        [Description("Priority (lower = higher priority)")]
        [DefaultValue(100)]
        public int Priority { get; set; } = 100;

        [CommandOption("--active-only")]
        [Description("Show only active markets")]
        public bool ActiveOnly { get; set; }

        [CommandOption("-f|--force")]
        [Description("Skip confirmation prompts")]
        public bool Force { get; set; }
    }
}
