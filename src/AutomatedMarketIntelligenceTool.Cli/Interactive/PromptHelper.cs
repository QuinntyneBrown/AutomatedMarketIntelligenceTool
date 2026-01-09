using Spectre.Console;

namespace AutomatedMarketIntelligenceTool.Cli.Interactive;

/// <summary>
/// Helper utilities for creating interactive prompts.
/// </summary>
public static class PromptHelper
{
    /// <summary>
    /// Prompts for a string value with optional default and auto-completion.
    /// </summary>
    public static string? PromptOptionalString(
        string prompt,
        string? defaultValue = null,
        IEnumerable<string>? suggestions = null)
    {
        var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
            .AllowEmpty();

        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.DefaultValue(defaultValue);
            textPrompt.ShowDefaultValue();
        }

        if (suggestions?.Any() == true)
        {
            textPrompt.AddChoices(suggestions);
            textPrompt.ShowChoices(false);
        }

        var result = AnsiConsole.Prompt(textPrompt);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    /// <summary>
    /// Prompts for a required string value.
    /// </summary>
    public static string PromptRequiredString(
        string prompt,
        string? defaultValue = null,
        IEnumerable<string>? suggestions = null)
    {
        var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]");

        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.DefaultValue(defaultValue);
            textPrompt.ShowDefaultValue();
        }

        if (suggestions?.Any() == true)
        {
            textPrompt.AddChoices(suggestions);
            textPrompt.ShowChoices(false);
        }

        return AnsiConsole.Prompt(textPrompt);
    }

    /// <summary>
    /// Prompts for an optional integer value.
    /// </summary>
    public static int? PromptOptionalInt(
        string prompt,
        int? defaultValue = null,
        int? min = null,
        int? max = null)
    {
        var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
            .AllowEmpty();

        if (defaultValue.HasValue)
        {
            textPrompt.DefaultValue(defaultValue.Value.ToString());
            textPrompt.ShowDefaultValue();
        }

        var result = AnsiConsole.Prompt(textPrompt);

        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        if (!int.TryParse(result, out var value))
        {
            AnsiConsole.MarkupLine("[yellow]Invalid number, skipping...[/]");
            return null;
        }

        if (min.HasValue && value < min.Value)
        {
            AnsiConsole.MarkupLine($"[yellow]Value must be at least {min.Value}, skipping...[/]");
            return null;
        }

        if (max.HasValue && value > max.Value)
        {
            AnsiConsole.MarkupLine($"[yellow]Value must be at most {max.Value}, skipping...[/]");
            return null;
        }

        return value;
    }

    /// <summary>
    /// Prompts for an optional decimal value.
    /// </summary>
    public static decimal? PromptOptionalDecimal(
        string prompt,
        decimal? defaultValue = null,
        decimal? min = null,
        decimal? max = null)
    {
        var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
            .AllowEmpty();

        if (defaultValue.HasValue)
        {
            textPrompt.DefaultValue(defaultValue.Value.ToString("N0"));
            textPrompt.ShowDefaultValue();
        }

        var result = AnsiConsole.Prompt(textPrompt);

        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        if (!decimal.TryParse(result.Replace(",", "").Replace("$", ""), out var value))
        {
            AnsiConsole.MarkupLine("[yellow]Invalid number, skipping...[/]");
            return null;
        }

        if (min.HasValue && value < min.Value)
        {
            AnsiConsole.MarkupLine($"[yellow]Value must be at least {min.Value:N0}, skipping...[/]");
            return null;
        }

        if (max.HasValue && value > max.Value)
        {
            AnsiConsole.MarkupLine($"[yellow]Value must be at most {max.Value:N0}, skipping...[/]");
            return null;
        }

        return value;
    }

    /// <summary>
    /// Prompts for a selection from a list of choices.
    /// </summary>
    public static T PromptSelection<T>(
        string prompt,
        IEnumerable<T> choices,
        T? defaultValue = default) where T : notnull
    {
        var selectionPrompt = new SelectionPrompt<T>()
            .Title($"[cyan]{prompt}[/]")
            .PageSize(10)
            .AddChoices(choices);

        return AnsiConsole.Prompt(selectionPrompt);
    }

    /// <summary>
    /// Prompts for multiple selections from a list of choices.
    /// </summary>
    public static IReadOnlyList<T> PromptMultiSelection<T>(
        string prompt,
        IEnumerable<T> choices,
        bool required = false) where T : notnull
    {
        var multiSelectionPrompt = new MultiSelectionPrompt<T>()
            .Title($"[cyan]{prompt}[/]")
            .PageSize(10)
            .Required(required)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
            .AddChoices(choices);

        return AnsiConsole.Prompt(multiSelectionPrompt);
    }

    /// <summary>
    /// Prompts for a confirmation.
    /// </summary>
    public static bool PromptConfirmation(string prompt, bool defaultValue = true)
    {
        return AnsiConsole.Confirm($"[cyan]{prompt}[/]", defaultValue);
    }

    /// <summary>
    /// Prompts for a Guid with validation.
    /// </summary>
    public static Guid? PromptOptionalGuid(string prompt, Guid? defaultValue = null)
    {
        var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
            .AllowEmpty();

        if (defaultValue.HasValue)
        {
            textPrompt.DefaultValue(defaultValue.Value.ToString());
            textPrompt.ShowDefaultValue();
        }

        var result = AnsiConsole.Prompt(textPrompt);

        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        if (!Guid.TryParse(result, out var value))
        {
            AnsiConsole.MarkupLine("[yellow]Invalid GUID format, skipping...[/]");
            return null;
        }

        return value;
    }

    /// <summary>
    /// Prompts for a required Guid with validation.
    /// </summary>
    public static Guid PromptRequiredGuid(string prompt, Guid? defaultValue = null)
    {
        while (true)
        {
            var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]");

            if (defaultValue.HasValue)
            {
                textPrompt.DefaultValue(defaultValue.Value.ToString());
                textPrompt.ShowDefaultValue();
            }

            var result = AnsiConsole.Prompt(textPrompt);

            if (Guid.TryParse(result, out var value))
            {
                return value;
            }

            AnsiConsole.MarkupLine("[red]Invalid GUID format. Please try again.[/]");
        }
    }

    /// <summary>
    /// Displays a horizontal rule with title.
    /// </summary>
    public static void DisplaySection(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold blue]{title}[/]").LeftJustified());
    }

    /// <summary>
    /// Displays a hint message.
    /// </summary>
    public static void DisplayHint(string message)
    {
        AnsiConsole.MarkupLine($"[grey]Hint: {message}[/]");
    }
}
