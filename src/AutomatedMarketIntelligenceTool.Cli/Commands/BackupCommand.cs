using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to create database backups.
/// </summary>
public class BackupCommand : AsyncCommand<BackupCommand.Settings>
{
    private readonly IBackupService _backupService;

    public BackupCommand(IBackupService backupService)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Check if user wants to list backups
            if (settings.List)
            {
                return await ListBackupsAsync();
            }

            // Check if user wants to cleanup old backups
            if (settings.Cleanup)
            {
                return await CleanupBackupsAsync(settings.RetentionCount, settings.Yes);
            }

            // Check for file overwrite confirmation
            if (!string.IsNullOrWhiteSpace(settings.OutputPath) && File.Exists(settings.OutputPath))
            {
                if (!AnsiConsole.Confirm($"File '{settings.OutputPath}' already exists. Overwrite?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Backup cancelled.[/]");
                    return ExitCodes.Success;
                }
            }

            // Perform backup with progress indicator
            BackupResult result = null!;
            
            await AnsiConsole.Status()
                .StartAsync("Creating database backup...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    result = await _backupService.BackupAsync(settings.OutputPath);
                });

            // Display results
            if (result.IsSuccess)
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn(new TableColumn("[bold]Property[/]"))
                    .AddColumn(new TableColumn("[bold]Value[/]"));

                table.AddRow("Backup File", result.BackupFilePath ?? "N/A");
                table.AddRow("File Size", $"{result.FileSizeBytes / (1024.0 * 1024.0):F2} MB");
                table.AddRow("Duration", $"{result.Duration.TotalSeconds:F2}s");

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[green]✓ Database backup created successfully![/]");
                
                return ExitCodes.Success;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Backup failed: {result.ErrorMessage}[/]");
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

    private async Task<int> ListBackupsAsync()
    {
        try
        {
            var backups = await _backupService.ListBackupsAsync();

            if (backups.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No backups found.[/]");
                return ExitCodes.Success;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]#[/]"))
                .AddColumn(new TableColumn("[bold]File Name[/]"))
                .AddColumn(new TableColumn("[bold]Size[/]"))
                .AddColumn(new TableColumn("[bold]Created[/]"));

            var index = 1;
            foreach (var backup in backups)
            {
                table.AddRow(
                    index.ToString(),
                    backup.FileName,
                    backup.FileSizeMB,
                    backup.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                );
                index++;
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]Total backups: {backups.Count}[/]");

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> CleanupBackupsAsync(int retentionCount, bool skipConfirmation)
    {
        try
        {
            // Get current backup count to show user what will be deleted
            var backups = await _backupService.ListBackupsAsync();
            var toDeleteCount = Math.Max(0, backups.Count - retentionCount);

            if (toDeleteCount == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No backups to clean up. Currently have {backups.Count} backup(s), retention is set to {retentionCount}.[/]");
                return ExitCodes.Success;
            }

            // Confirm deletion unless --yes flag is provided
            if (!skipConfirmation)
            {
                var panel = new Panel(
                    $"[yellow]This will delete {toDeleteCount} backup file(s), keeping the {retentionCount} most recent.[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Header("[bold yellow]Backup Cleanup[/]");

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                if (!AnsiConsole.Confirm("Are you sure you want to delete these backups?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Cleanup cancelled.[/]");
                    return ExitCodes.Success;
                }
            }

            AnsiConsole.MarkupLine($"[dim]Cleaning up old backups (keeping {retentionCount} most recent)...[/]");

            var deletedCount = await _backupService.CleanupOldBackupsAsync(retentionCount);

            if (deletedCount > 0)
            {
                AnsiConsole.MarkupLine($"[green]✓ Deleted {deletedCount} old backup file(s).[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]No backups to clean up.[/]");
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return ExitCodes.GeneralError;
        }
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-o|--output")]
        [Description("Output file path for backup (optional, uses default location if not specified)")]
        public string? OutputPath { get; set; }

        [CommandOption("-l|--list")]
        [Description("List available backups")]
        [DefaultValue(false)]
        public bool List { get; set; }

        [CommandOption("--cleanup")]
        [Description("Clean up old backup files according to retention policy")]
        [DefaultValue(false)]
        public bool Cleanup { get; set; }

        [CommandOption("--retention")]
        [Description("Number of backups to keep when cleaning up (default: 5)")]
        [DefaultValue(5)]
        public int RetentionCount { get; set; } = 5;

        [CommandOption("-y|--yes")]
        [Description("Skip confirmation prompt for cleanup operation")]
        [DefaultValue(false)]
        public bool Yes { get; set; }
    }
}
