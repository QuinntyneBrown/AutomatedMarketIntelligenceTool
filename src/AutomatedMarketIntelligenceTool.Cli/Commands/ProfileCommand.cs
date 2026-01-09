using System.ComponentModel;
using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage search profiles (save, load, list, delete).
/// </summary>
public class ProfileCommand : AsyncCommand<ProfileCommand.Settings>
{
    private readonly ISearchProfileService _profileService;

    public ProfileCommand(ISearchProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.Action.ToLowerInvariant() switch
            {
                "save" => await SaveProfileAsync(settings),
                "load" => await LoadProfileAsync(settings),
                "list" => await ListProfilesAsync(settings),
                "delete" => await DeleteProfileAsync(settings),
                _ => ShowHelp()
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> SaveProfileAsync(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.ProfileName))
        {
            AnsiConsole.MarkupLine("[red]Error: Profile name is required for save action[/]");
            return ExitCodes.ValidationError;
        }

        // Build search parameters JSON
        var searchParams = new Dictionary<string, object?>();
        
        if (!string.IsNullOrEmpty(settings.Make))
            searchParams["make"] = settings.Make;
        if (!string.IsNullOrEmpty(settings.Model))
            searchParams["model"] = settings.Model;
        if (settings.YearMin.HasValue)
            searchParams["yearMin"] = settings.YearMin;
        if (settings.YearMax.HasValue)
            searchParams["yearMax"] = settings.YearMax;
        if (settings.PriceMin.HasValue)
            searchParams["priceMin"] = settings.PriceMin;
        if (settings.PriceMax.HasValue)
            searchParams["priceMax"] = settings.PriceMax;
        if (settings.MileageMax.HasValue)
            searchParams["mileageMax"] = settings.MileageMax;
        if (!string.IsNullOrEmpty(settings.Condition))
            searchParams["condition"] = settings.Condition;
        if (!string.IsNullOrEmpty(settings.BodyStyle))
            searchParams["bodyStyle"] = settings.BodyStyle;
        if (!string.IsNullOrEmpty(settings.ExteriorColor))
            searchParams["exteriorColor"] = settings.ExteriorColor;
        if (!string.IsNullOrEmpty(settings.InteriorColor))
            searchParams["interiorColor"] = settings.InteriorColor;
        if (!string.IsNullOrEmpty(settings.Drivetrain))
            searchParams["drivetrain"] = settings.Drivetrain;

        var searchParamsJson = JsonSerializer.Serialize(searchParams, new JsonSerializerOptions { WriteIndented = true });
        
        var profile = await _profileService.SaveProfileAsync(
            settings.TenantId,
            settings.ProfileName,
            searchParamsJson,
            settings.Description);

        AnsiConsole.MarkupLine($"[green]✓[/] Profile '{profile.Name}' saved successfully");
        
        if (!string.IsNullOrEmpty(settings.Description))
        {
            AnsiConsole.MarkupLine($"  Description: {settings.Description}");
        }

        return ExitCodes.Success;
    }

    private async Task<int> LoadProfileAsync(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.ProfileName))
        {
            AnsiConsole.MarkupLine("[red]Error: Profile name is required for load action[/]");
            return ExitCodes.ValidationError;
        }

        var profile = await _profileService.LoadProfileAsync(settings.TenantId, settings.ProfileName);

        if (profile == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Profile '{settings.ProfileName}' not found[/]");
            return ExitCodes.NotFound;
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Profile '{profile.Name}' loaded");
        
        if (!string.IsNullOrEmpty(profile.Description))
        {
            AnsiConsole.MarkupLine($"  Description: {profile.Description}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Search Parameters:[/]");
        
        var searchParams = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(profile.SearchParametersJson);
        if (searchParams != null)
        {
            foreach (var (key, value) in searchParams)
            {
                AnsiConsole.MarkupLine($"  {key}: {value}");
            }
        }

        return ExitCodes.Success;
    }

    private async Task<int> ListProfilesAsync(Settings settings)
    {
        var profiles = await _profileService.ListProfilesAsync(settings.TenantId);

        if (profiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No saved profiles found[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Profile Name");
        table.AddColumn("Description");
        table.AddColumn("Last Used");
        table.AddColumn("Created");

        foreach (var profile in profiles)
        {
            table.AddRow(
                profile.Name,
                profile.Description ?? "-",
                profile.LastUsedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never",
                profile.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[green]{profiles.Count}[/] profile(s) found");

        return ExitCodes.Success;
    }

    private async Task<int> DeleteProfileAsync(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.ProfileName))
        {
            AnsiConsole.MarkupLine("[red]Error: Profile name is required for delete action[/]");
            return ExitCodes.ValidationError;
        }

        var deleted = await _profileService.DeleteProfileAsync(settings.TenantId, settings.ProfileName);

        if (!deleted)
        {
            AnsiConsole.MarkupLine($"[red]Error: Profile '{settings.ProfileName}' not found[/]");
            return ExitCodes.NotFound;
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Profile '{settings.ProfileName}' deleted successfully");
        return ExitCodes.Success;
    }

    private static int ShowHelp()
    {
        AnsiConsole.MarkupLine("[red]Error: Invalid action. Valid actions are: save, load, list, delete[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  car-search profile save family-suv -t <tenant-id> -m Toyota,Honda --body-style suv --price-max 40000");
        AnsiConsole.MarkupLine("  car-search profile load family-suv -t <tenant-id>");
        AnsiConsole.MarkupLine("  car-search profile list -t <tenant-id>");
        AnsiConsole.MarkupLine("  car-search profile delete family-suv -t <tenant-id>");
        return ExitCodes.ValidationError;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<action>")]
        [Description("Action to perform: save, load, list, or delete")]
        public string Action { get; set; } = string.Empty;

        [CommandArgument(1, "[profile-name]")]
        [Description("Name of the profile (required for save, load, and delete)")]
        public string? ProfileName { get; set; }

        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("-d|--description <TEXT>")]
        [Description("Description of the profile (for save action)")]
        public string? Description { get; set; }

        // Search parameters for save action
        [CommandOption("-m|--make <MAKES>")]
        [Description("Vehicle make(s), comma-separated")]
        public string? Make { get; set; }

        [CommandOption("--model <MODELS>")]
        [Description("Vehicle model(s), comma-separated")]
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
        [Description("Vehicle condition")]
        public string? Condition { get; set; }

        [CommandOption("--body-style <STYLE>")]
        [Description("Body style")]
        public string? BodyStyle { get; set; }

        [CommandOption("--exterior-color <COLOR>")]
        [Description("Exterior color")]
        public string? ExteriorColor { get; set; }

        [CommandOption("--interior-color <COLOR>")]
        [Description("Interior color")]
        public string? InteriorColor { get; set; }

        [CommandOption("--drivetrain <DRIVETRAIN>")]
        [Description("Drivetrain type")]
        public string? Drivetrain { get; set; }
    }
}
