using AutomatedMarketIntelligenceTool.Cli.Commands;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AutomatedMarketIntelligenceTool.Cli.Interactive;

/// <summary>
/// Provides interactive mode for building search queries.
/// </summary>
public class InteractiveMode
{
    private readonly IAutoCompleteService _autoCompleteService;
    private readonly ILogger<InteractiveMode> _logger;
    private readonly MakeModelAutoComplete _makeModelAutoComplete;

    // Store previous values for repeat searches
    private static SearchCommand.Settings? _lastSettings;

    public InteractiveMode(
        IAutoCompleteService autoCompleteService,
        ILogger<InteractiveMode> logger)
    {
        _autoCompleteService = autoCompleteService ?? throw new ArgumentNullException(nameof(autoCompleteService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _makeModelAutoComplete = new MakeModelAutoComplete(autoCompleteService);
    }

    /// <summary>
    /// Runs interactive mode to build search settings.
    /// </summary>
    public async Task<SearchCommand.Settings> BuildSearchSettingsAsync(
        SearchCommand.Settings existingSettings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting interactive mode for search settings");

        DisplayWelcome();

        var settings = new SearchCommand.Settings();

        // Use previous settings as defaults if available
        var defaults = _lastSettings ?? existingSettings;

        // Section 1: Tenant ID (required)
        PromptHelper.DisplaySection("Authentication");
        settings.TenantId = PromptForTenantId(defaults.TenantId);

        // Section 2: Vehicle Make/Model
        PromptHelper.DisplaySection("Vehicle Selection");
        settings.Makes = await PromptForMakesAsync(settings.TenantId, defaults.Makes, cancellationToken);
        settings.Models = await PromptForModelsAsync(settings.TenantId, settings.Makes, defaults.Models, cancellationToken);

        // Section 3: Year Range
        PromptHelper.DisplaySection("Year Range");
        var (minYear, maxYear) = await GetYearRangeAsync(settings.TenantId, cancellationToken);
        settings.YearMin = PromptForYear("Minimum year", defaults.YearMin, minYear, maxYear);
        settings.YearMax = PromptForYear("Maximum year", defaults.YearMax, settings.YearMin ?? minYear, maxYear);

        // Section 4: Price Range
        PromptHelper.DisplaySection("Price Range");
        var (minPrice, maxPrice) = await GetPriceRangeAsync(settings.TenantId, cancellationToken);
        settings.PriceMin = PromptForPrice("Minimum price", defaults.PriceMin, minPrice, maxPrice);
        settings.PriceMax = PromptForPrice("Maximum price", defaults.PriceMax, settings.PriceMin ?? minPrice, maxPrice);

        // Section 5: Mileage Range
        PromptHelper.DisplaySection("Mileage Range");
        settings.MileageMin = PromptHelper.PromptOptionalInt(
            "Minimum mileage (km)",
            defaults.MileageMin,
            min: 0);
        settings.MileageMax = PromptHelper.PromptOptionalInt(
            "Maximum mileage (km)",
            defaults.MileageMax,
            min: settings.MileageMin ?? 0);

        // Section 6: Location
        PromptHelper.DisplaySection("Location");
        settings.ZipCode = PromptHelper.PromptOptionalString(
            "Postal code (e.g., M5V 3L9)",
            defaults.ZipCode);

        if (!string.IsNullOrEmpty(settings.ZipCode))
        {
            settings.Radius = PromptHelper.PromptOptionalInt(
                "Search radius (km)",
                (int?)defaults.Radius ?? 40,
                min: 1,
                max: 500) ?? 40;
        }

        // Section 7: Output Options
        PromptHelper.DisplaySection("Output Options");
        settings.Format = PromptForFormat(defaults.Format);
        settings.PageSize = PromptHelper.PromptOptionalInt(
            "Results per page",
            defaults.PageSize,
            min: 1,
            max: 100) ?? 30;

        // Display final command preview
        DisplayCommandPreview(settings);

        // Confirm execution
        AnsiConsole.WriteLine();
        var execute = PromptHelper.PromptConfirmation("Execute this search?", defaultValue: true);

        if (!execute)
        {
            throw new OperationCanceledException("Search cancelled by user.");
        }

        // Save settings for next time
        _lastSettings = settings;

        _logger.LogDebug("Interactive mode completed, settings configured");
        return settings;
    }

    private void DisplayWelcome()
    {
        AnsiConsole.Clear();

        var panel = new Panel(
            new Markup("[bold]Welcome to Interactive Search Mode[/]\n\n" +
                      "Guide through search parameters step by step.\n" +
                      "[grey]Press Enter to skip optional fields.[/]"))
        {
            Header = new PanelHeader("[bold blue]Car Search[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private static Guid PromptForTenantId(Guid defaultValue)
    {
        if (defaultValue != Guid.Empty)
        {
            var useExisting = PromptHelper.PromptConfirmation(
                $"Use existing Tenant ID: {defaultValue}?",
                defaultValue: true);

            if (useExisting)
            {
                return defaultValue;
            }
        }

        return PromptHelper.PromptRequiredGuid("Tenant ID");
    }

    private async Task<string[]?> PromptForMakesAsync(
        Guid tenantId,
        string[]? previousValues,
        CancellationToken cancellationToken)
    {
        return await _makeModelAutoComplete.PromptForMakesAsync(
            tenantId,
            previousValues,
            cancellationToken);
    }

    private async Task<string[]?> PromptForModelsAsync(
        Guid tenantId,
        string[]? makes,
        string[]? previousValues,
        CancellationToken cancellationToken)
    {
        return await _makeModelAutoComplete.PromptForModelsAsync(
            tenantId,
            makes,
            previousValues,
            cancellationToken);
    }

    private async Task<(int MinYear, int MaxYear)> GetYearRangeAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _autoCompleteService.GetYearRangeAsync(tenantId, cancellationToken);
        }
        catch
        {
            var currentYear = DateTime.UtcNow.Year;
            return (currentYear - 20, currentYear + 1);
        }
    }

    private async Task<(decimal MinPrice, decimal MaxPrice)> GetPriceRangeAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _autoCompleteService.GetPriceRangeAsync(tenantId, cancellationToken);
        }
        catch
        {
            return (0m, 200000m);
        }
    }

    private static int? PromptForYear(string prompt, int? defaultValue, int minYear, int maxYear)
    {
        PromptHelper.DisplayHint($"Available range: {minYear} - {maxYear}");
        return PromptHelper.PromptOptionalInt(prompt, defaultValue, min: minYear, max: maxYear);
    }

    private static decimal? PromptForPrice(string prompt, decimal? defaultValue, decimal minPrice, decimal maxPrice)
    {
        PromptHelper.DisplayHint($"Available range: ${minPrice:N0} - ${maxPrice:N0}");
        return PromptHelper.PromptOptionalDecimal(prompt, defaultValue, min: 0);
    }

    private static string PromptForFormat(string defaultValue)
    {
        var formats = new[] { "table", "json", "csv" };

        return PromptHelper.PromptSelection(
            "Output format",
            formats,
            defaultValue);
    }

    private static void DisplayCommandPreview(SearchCommand.Settings settings)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Command Preview[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var commandParts = new List<string> { "car-search", "search" };

        commandParts.Add($"-t {settings.TenantId}");

        if (settings.Makes?.Any() == true)
        {
            commandParts.Add($"-m {string.Join(" ", settings.Makes.Select(m => m.Contains(' ') ? $"\"{m}\"" : m))}");
        }

        if (settings.Models?.Any() == true)
        {
            commandParts.Add($"--model {string.Join(" ", settings.Models.Select(m => m.Contains(' ') ? $"\"{m}\"" : m))}");
        }

        if (settings.YearMin.HasValue)
        {
            commandParts.Add($"--year-min {settings.YearMin}");
        }

        if (settings.YearMax.HasValue)
        {
            commandParts.Add($"--year-max {settings.YearMax}");
        }

        if (settings.PriceMin.HasValue)
        {
            commandParts.Add($"--price-min {settings.PriceMin}");
        }

        if (settings.PriceMax.HasValue)
        {
            commandParts.Add($"--price-max {settings.PriceMax}");
        }

        if (settings.MileageMin.HasValue)
        {
            commandParts.Add($"--mileage-min {settings.MileageMin}");
        }

        if (settings.MileageMax.HasValue)
        {
            commandParts.Add($"--mileage-max {settings.MileageMax}");
        }

        if (!string.IsNullOrEmpty(settings.ZipCode))
        {
            commandParts.Add($"-p \"{settings.ZipCode}\"");
            commandParts.Add($"-r {settings.Radius}");
        }

        if (settings.Format != "table")
        {
            commandParts.Add($"-f {settings.Format}");
        }

        if (settings.PageSize != 30)
        {
            commandParts.Add($"--page-size {settings.PageSize}");
        }

        var command = string.Join(" ", commandParts);

        var panel = new Panel(new Markup($"[yellow]{command}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(1, 0)
        };

        AnsiConsole.Write(panel);

        PromptHelper.DisplayHint("You can copy this command for future use without interactive mode.");
    }

    /// <summary>
    /// Clears the saved previous settings.
    /// </summary>
    public static void ClearPreviousSettings()
    {
        _lastSettings = null;
    }
}
