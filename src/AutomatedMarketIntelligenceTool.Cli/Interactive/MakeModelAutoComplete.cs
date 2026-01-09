using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;

namespace AutomatedMarketIntelligenceTool.Cli.Interactive;

/// <summary>
/// Provides auto-completion for make and model inputs.
/// </summary>
public class MakeModelAutoComplete
{
    private readonly IAutoCompleteService _autoCompleteService;

    public MakeModelAutoComplete(IAutoCompleteService autoCompleteService)
    {
        _autoCompleteService = autoCompleteService ?? throw new ArgumentNullException(nameof(autoCompleteService));
    }

    /// <summary>
    /// Prompts for vehicle make with auto-completion.
    /// </summary>
    public async Task<string[]?> PromptForMakesAsync(
        Guid tenantId,
        string[]? previousValues = null,
        CancellationToken cancellationToken = default)
    {
        var suggestions = await _autoCompleteService.GetMakeSuggestionsAsync(
            tenantId,
            prefix: null,
            cancellationToken);

        if (!suggestions.Any())
        {
            PromptHelper.DisplayHint("No existing makes found. Enter make names manually.");
            return PromptForCommaSeparatedValues("Vehicle make(s)", previousValues);
        }

        PromptHelper.DisplayHint("Select from suggestions or type custom values.");

        var selectedMakes = new List<string>();

        // Multi-select from existing makes
        if (suggestions.Count > 0)
        {
            var useSelection = PromptHelper.PromptConfirmation("Select from existing makes?", defaultValue: true);

            if (useSelection)
            {
                var selected = PromptHelper.PromptMultiSelection(
                    "Select vehicle make(s) (space to select, enter to confirm)",
                    suggestions,
                    required: false);

                selectedMakes.AddRange(selected);
            }
        }

        // Allow additional custom makes
        if (PromptHelper.PromptConfirmation("Add additional makes manually?", defaultValue: false))
        {
            var customMakes = PromptForCommaSeparatedValues("Additional make(s)", null);
            if (customMakes != null)
            {
                selectedMakes.AddRange(customMakes);
            }
        }

        return selectedMakes.Count > 0 ? selectedMakes.Distinct().ToArray() : null;
    }

    /// <summary>
    /// Prompts for vehicle model with auto-completion based on selected make.
    /// </summary>
    public async Task<string[]?> PromptForModelsAsync(
        Guid tenantId,
        string[]? makes,
        string[]? previousValues = null,
        CancellationToken cancellationToken = default)
    {
        var selectedModels = new List<string>();

        // If we have a single make, filter models by that make
        var makeName = makes?.Length == 1 ? makes[0] : null;

        var suggestions = await _autoCompleteService.GetModelSuggestionsAsync(
            tenantId,
            make: makeName,
            prefix: null,
            cancellationToken);

        if (!suggestions.Any())
        {
            PromptHelper.DisplayHint("No existing models found. Enter model names manually.");
            return PromptForCommaSeparatedValues("Vehicle model(s)", previousValues);
        }

        PromptHelper.DisplayHint(makeName != null
            ? $"Showing models for {makeName}. Select or enter custom values."
            : "Select from suggestions or type custom values.");

        if (suggestions.Count > 0)
        {
            var useSelection = PromptHelper.PromptConfirmation("Select from existing models?", defaultValue: true);

            if (useSelection)
            {
                var selected = PromptHelper.PromptMultiSelection(
                    "Select vehicle model(s) (space to select, enter to confirm)",
                    suggestions,
                    required: false);

                selectedModels.AddRange(selected);
            }
        }

        // Allow additional custom models
        if (PromptHelper.PromptConfirmation("Add additional models manually?", defaultValue: false))
        {
            var customModels = PromptForCommaSeparatedValues("Additional model(s)", null);
            if (customModels != null)
            {
                selectedModels.AddRange(customModels);
            }
        }

        return selectedModels.Count > 0 ? selectedModels.Distinct().ToArray() : null;
    }

    /// <summary>
    /// Prompts for comma-separated values.
    /// </summary>
    private static string[]? PromptForCommaSeparatedValues(string prompt, string[]? previousValues)
    {
        var defaultValue = previousValues != null ? string.Join(", ", previousValues) : null;

        var textPrompt = new TextPrompt<string>($"[cyan]{prompt} (comma-separated)[/]")
            .AllowEmpty();

        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.DefaultValue(defaultValue);
            textPrompt.ShowDefaultValue();
        }

        var result = AnsiConsole.Prompt(textPrompt);

        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        return result
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }
}
