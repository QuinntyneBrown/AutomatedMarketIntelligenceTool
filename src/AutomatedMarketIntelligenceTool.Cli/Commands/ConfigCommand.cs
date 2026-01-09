using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Cli.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage application configuration settings.
/// </summary>
public class ConfigCommand : Command<ConfigCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var configManager = string.IsNullOrEmpty(settings.ConfigFile)
                ? new ConfigurationManager()
                : new ConfigurationManager(settings.ConfigFile);

            switch (settings.Operation?.ToLower())
            {
                case "list":
                    return ListConfig(configManager);
                
                case "get":
                    if (string.IsNullOrEmpty(settings.Key))
                    {
                        AnsiConsole.MarkupLine("[red]Error: Key is required for 'get' operation[/]");
                        return ExitCodes.ValidationError;
                    }
                    return GetConfig(configManager, settings.Key);
                
                case "set":
                    if (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.Value))
                    {
                        AnsiConsole.MarkupLine("[red]Error: Key and value are required for 'set' operation[/]");
                        return ExitCodes.ValidationError;
                    }
                    return SetConfig(configManager, settings.Key, settings.Value);
                
                case "reset":
                    return ResetConfig(configManager);
                
                case "path":
                    return ShowConfigPath(configManager);
                
                default:
                    AnsiConsole.MarkupLine("[red]Error: Invalid operation. Valid operations are: list, get, set, reset, path[/]");
                    return ExitCodes.ValidationError;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private int ListConfig(ConfigurationManager configManager)
    {
        var allSettings = configManager.GetAllSettings();

        if (allSettings.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No configuration settings found.[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]Setting[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        foreach (var kvp in allSettings.OrderBy(x => x.Key))
        {
            table.AddRow(kvp.Key, kvp.Value);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Configuration file: {configManager.GetConfigFilePath()}[/]");

        return ExitCodes.Success;
    }

    private int GetConfig(ConfigurationManager configManager, string key)
    {
        var value = configManager.GetValue(key);

        if (value == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Configuration key '{key}' not found[/]");
            return ExitCodes.ValidationError;
        }

        AnsiConsole.MarkupLine($"[green]{key}[/] = [yellow]{value}[/]");
        return ExitCodes.Success;
    }

    private int SetConfig(ConfigurationManager configManager, string key, string value)
    {
        var success = configManager.SetValue(key, value);

        if (!success)
        {
            AnsiConsole.MarkupLine($"[red]Error: Failed to set configuration key '{key}'. Check that the key is valid and the value is the correct type.[/]");
            return ExitCodes.ValidationError;
        }

        AnsiConsole.MarkupLine($"[green]Successfully set {key} = {value}[/]");
        return ExitCodes.Success;
    }

    private int ResetConfig(ConfigurationManager configManager)
    {
        if (!AnsiConsole.Confirm("Are you sure you want to reset all configuration to default values?"))
        {
            AnsiConsole.MarkupLine("[yellow]Reset cancelled.[/]");
            return ExitCodes.Success;
        }

        configManager.ResetToDefaults();
        AnsiConsole.MarkupLine("[green]Configuration reset to default values.[/]");
        return ExitCodes.Success;
    }

    private int ShowConfigPath(ConfigurationManager configManager)
    {
        AnsiConsole.MarkupLine($"[green]Configuration file path:[/] {configManager.GetConfigFilePath()}");
        return ExitCodes.Success;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<operation>")]
        [Description("Operation: list, get, set, reset, path")]
        public string? Operation { get; set; }

        [CommandArgument(1, "[key]")]
        [Description("Configuration key (for get/set operations, e.g., 'Database:Provider')")]
        public string? Key { get; set; }

        [CommandArgument(2, "[value]")]
        [Description("Configuration value (for set operation)")]
        public string? Value { get; set; }

        [CommandOption("--config")]
        [Description("Path to custom configuration file")]
        public string? ConfigFile { get; set; }
    }
}
