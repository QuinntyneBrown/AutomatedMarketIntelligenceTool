using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to restore database from a backup file.
/// </summary>
public class RestoreCommand : AsyncCommand<RestoreCommand.Settings>
{
    private readonly IBackupService _backupService;

    public RestoreCommand(IBackupService backupService)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Validate backup file exists
            if (!File.Exists(settings.BackupFilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: Backup file not found: {settings.BackupFilePath}[/]");
                return ExitCodes.ValidationError;
            }

            // Display warning
            var panel = new Panel(
                "[yellow]WARNING: This operation will replace your current database with the backup.[/]\n" +
                "[yellow]A backup of the current database will be created automatically before restoration.[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Yellow)
                .Header("[bold red]⚠ Database Restore[/]");

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Confirm unless --yes flag is provided
            if (!settings.Yes)
            {
                if (!AnsiConsole.Confirm("Are you sure you want to restore from this backup?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Restore cancelled.[/]");
                    return ExitCodes.Success;
                }
            }

            // Display restore information
            var fileInfo = new FileInfo(settings.BackupFilePath);
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]Property[/]"))
                .AddColumn(new TableColumn("[bold]Value[/]"));

            table.AddRow("Backup File", settings.BackupFilePath);
            table.AddRow("File Size", $"{fileInfo.Length / (1024.0 * 1024.0):F2} MB");
            table.AddRow("Created", fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Perform restore with progress indicator
            RestoreResult result = null!;
            
            await AnsiConsole.Status()
                .StartAsync("Restoring database from backup...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    result = await _backupService.RestoreAsync(settings.BackupFilePath);
                });

            // Display results
            if (result.IsSuccess)
            {
                var resultTable = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn(new TableColumn("[bold]Property[/]"))
                    .AddColumn(new TableColumn("[bold]Value[/]"));

                resultTable.AddRow("Duration", $"{result.Duration.TotalSeconds:F2}s");
                resultTable.AddRow("Status", "[green]Success[/]");

                AnsiConsole.Write(resultTable);
                AnsiConsole.MarkupLine($"\n[green]✓ Database restored successfully![/]");
                AnsiConsole.MarkupLine($"[dim]A pre-restore backup was created automatically.[/]");
                
                return ExitCodes.Success;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Restore failed: {result.ErrorMessage}[/]");
                return ExitCodes.GeneralError;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<backup-file>")]
        [Description("Path to the backup file to restore from")]
        public string BackupFilePath { get; set; } = string.Empty;

        [CommandOption("-y|--yes")]
        [Description("Skip confirmation prompt")]
        [DefaultValue(false)]
        public bool Yes { get; set; }
    }
}
