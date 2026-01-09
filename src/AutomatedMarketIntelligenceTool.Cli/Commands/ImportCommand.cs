using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to import car listings from CSV or JSON files.
/// </summary>
public class ImportCommand : AsyncCommand<ImportCommand.Settings>
{
    private readonly IDataImportService _importService;

    public ImportCommand(IDataImportService importService)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Validate file exists
            if (!File.Exists(settings.FilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: File not found: {settings.FilePath}[/]");
                return ExitCodes.ValidationError;
            }

            // Determine format
            var format = DetermineFormat(settings.FilePath, settings.Format);
            if (format == null)
            {
                AnsiConsole.MarkupLine("[red]Error: Could not determine import format. Please specify --format or use a .csv or .json file extension.[/]");
                return ExitCodes.ValidationError;
            }

            // Display import information
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]Setting[/]"))
                .AddColumn(new TableColumn("[bold]Value[/]"));

            table.AddRow("File", settings.FilePath);
            table.AddRow("Format", format!.Value.ToString().ToUpper());
            table.AddRow("Tenant ID", settings.TenantId.ToString());
            table.AddRow("Mode", settings.DryRun ? "[yellow]DRY RUN[/]" : "[green]LIVE[/]");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Validate file first
            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]Validating import file...[/]");
                var validation = await _importService.ValidateImportFileAsync(settings.FilePath, format.Value);
                
                if (!validation.IsValid)
                {
                    AnsiConsole.MarkupLine("[red]Validation failed:[/]");
                    foreach (var error in validation.Errors)
                    {
                        AnsiConsole.MarkupLine($"  [red]• {error}[/]");
                    }
                    return ExitCodes.ValidationError;
                }

                AnsiConsole.MarkupLine($"[green]✓ File is valid. Estimated {validation.EstimatedRowCount} records.[/]");
                AnsiConsole.WriteLine();
            }

            // Perform import with progress indicator
            ImportResult result = null!;
            
            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[green]Importing from {format!.Value.ToString().ToUpper()}...[/]");
                    task.IsIndeterminate = true;

                    if (format == ImportFormat.Csv)
                    {
                        result = await _importService.ImportFromCsvAsync(settings.FilePath, settings.TenantId, settings.DryRun);
                    }
                    else if (format == ImportFormat.Json)
                    {
                        result = await _importService.ImportFromJsonAsync(settings.FilePath, settings.TenantId, settings.DryRun);
                    }

                    task.Value = 100;
                    task.StopTask();
                });

            // Display results
            AnsiConsole.WriteLine();
            DisplayImportResults(result);

            // Display errors if any
            if (result.ErrorCount > 0 && settings.ShowErrors)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red bold]Errors:[/]");
                
                var errorTable = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn(new TableColumn("[bold]Line[/]"))
                    .AddColumn(new TableColumn("[bold]Error[/]"));

                foreach (var error in result.Errors.Take(settings.MaxErrorsToShow))
                {
                    errorTable.AddRow(
                        error.LineNumber.ToString(),
                        error.ErrorMessage
                    );
                }

                AnsiConsole.Write(errorTable);
                
                if (result.Errors.Count > settings.MaxErrorsToShow)
                {
                    AnsiConsole.MarkupLine($"[yellow]... and {result.Errors.Count - settings.MaxErrorsToShow} more errors[/]");
                }
            }

            return result.IsSuccess ? ExitCodes.Success : ExitCodes.GeneralError;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private ImportFormat? DetermineFormat(string filePath, string? explicitFormat)
    {
        if (!string.IsNullOrEmpty(explicitFormat))
        {
            return Enum.TryParse<ImportFormat>(explicitFormat, true, out var format) ? format : null;
        }

        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".csv" => ImportFormat.Csv,
            ".json" => ImportFormat.Json,
            _ => null
        };
    }

    private void DisplayImportResults(ImportResult result)
    {
        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Metric[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        summaryTable.AddRow("Total Rows", result.TotalRows.ToString());
        summaryTable.AddRow("[green]Successfully Imported[/]", $"[green]{result.SuccessCount}[/]");
        summaryTable.AddRow("[yellow]Skipped (Duplicates)[/]", $"[yellow]{result.SkippedCount}[/]");
        summaryTable.AddRow("[red]Errors[/]", result.ErrorCount > 0 ? $"[red]{result.ErrorCount}[/]" : "0");
        summaryTable.AddRow("Duration", $"{result.Duration.TotalSeconds:F2}s");
        
        if (result.IsDryRun)
        {
            summaryTable.AddRow("Mode", "[yellow]DRY RUN - No changes were made[/]");
        }

        AnsiConsole.Write(summaryTable);

        if (result.IsSuccess)
        {
            if (result.IsDryRun)
            {
                AnsiConsole.MarkupLine($"\n[yellow]✓ Dry run completed successfully. {result.SuccessCount} records would be imported.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"\n[green]✓ Import completed successfully. {result.SuccessCount} records imported.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"\n[red]✗ Import completed with {result.ErrorCount} errors.[/]");
        }
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file-path>")]
        [Description("Path to the CSV or JSON file to import")]
        public string FilePath { get; set; } = string.Empty;

        [CommandOption("-f|--format")]
        [Description("Import format: csv, json (auto-detected from extension if not specified)")]
        public string? Format { get; set; }

        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("--dry-run")]
        [Description("Preview import without making changes")]
        [DefaultValue(false)]
        public bool DryRun { get; set; }

        [CommandOption("--show-errors")]
        [Description("Display import errors in detail")]
        [DefaultValue(true)]
        public bool ShowErrors { get; set; } = true;

        [CommandOption("--max-errors")]
        [Description("Maximum number of errors to display")]
        [DefaultValue(10)]
        public int MaxErrorsToShow { get; set; } = 10;
    }
}
