using System.ComponentModel;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Scheduling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to manage scheduled reports.
/// </summary>
public class ScheduleCommand : AsyncCommand<ScheduleCommand.Settings>
{
    private readonly IScheduledReportService _scheduledReportService;

    public ScheduleCommand(IScheduledReportService scheduledReportService)
    {
        _scheduledReportService = scheduledReportService ?? throw new ArgumentNullException(nameof(scheduledReportService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.SubCommand?.ToLowerInvariant() switch
            {
                "create" or "add" => await CreateScheduleAsync(settings),
                "update" or "edit" => await UpdateScheduleAsync(settings),
                "delete" or "remove" => await DeleteScheduleAsync(settings),
                "pause" => await PauseScheduleAsync(settings),
                "resume" => await ResumeScheduleAsync(settings),
                "cancel" => await CancelScheduleAsync(settings),
                "run" or "run-now" => await RunNowAsync(settings),
                "show" or "get" => await ShowScheduleAsync(settings),
                _ => await ListSchedulesAsync(settings)
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> ListSchedulesAsync(Settings settings)
    {
        AnsiConsole.Status()
            .Start("Loading scheduled reports...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        ScheduledReportStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(settings.Status))
        {
            if (Enum.TryParse<ScheduledReportStatus>(settings.Status, true, out var parsed))
                statusFilter = parsed;
        }

        var schedules = await _scheduledReportService.GetAllScheduledReportsAsync(settings.TenantId, statusFilter);

        if (schedules.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No scheduled reports found[/]");
            return ExitCodes.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Format[/]");
        table.AddColumn("[bold]Schedule[/]");
        table.AddColumn("[bold]Time[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Next Run[/]");
        table.AddColumn("[bold]Runs[/]");

        foreach (var schedule in schedules)
        {
            var statusColor = schedule.Status switch
            {
                ScheduledReportStatus.Active => "green",
                ScheduledReportStatus.Paused => "yellow",
                ScheduledReportStatus.Completed => "blue",
                ScheduledReportStatus.Cancelled => "dim",
                ScheduledReportStatus.Error => "red",
                _ => "white"
            };

            var nextRun = schedule.NextRunAt.HasValue
                ? schedule.NextRunAt.Value.ToString("yyyy-MM-dd HH:mm")
                : "-";

            table.AddRow(
                schedule.Name.Length > 20 ? schedule.Name[..17] + "..." : schedule.Name,
                schedule.Format.ToString(),
                schedule.Schedule.ToString(),
                schedule.ScheduledTime.ToString(@"hh\:mm"),
                $"[{statusColor}]{schedule.Status}[/]",
                nextRun,
                $"{schedule.SuccessfulRuns}/{schedule.SuccessfulRuns + schedule.FailedRuns}"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {schedules.Count} schedule(s)[/]");

        return ExitCodes.Success;
    }

    private async Task<int> CreateScheduleAsync(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: --name is required[/]");
            return ExitCodes.ValidationError;
        }

        if (string.IsNullOrWhiteSpace(settings.OutputDir))
        {
            AnsiConsole.MarkupLine("[red]Error: --output-dir is required[/]");
            return ExitCodes.ValidationError;
        }

        if (!Enum.TryParse<ReportFormat>(settings.Format, true, out var format))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid format. Use html, pdf, or excel[/]");
            return ExitCodes.ValidationError;
        }

        if (!Enum.TryParse<ReportSchedule>(settings.ScheduleType, true, out var scheduleType))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid schedule. Use once, daily, weekly, biweekly, monthly, or quarterly[/]");
            return ExitCodes.ValidationError;
        }

        if (!TimeSpan.TryParse(settings.Time, out var scheduledTime))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid time format. Use HH:mm[/]");
            return ExitCodes.ValidationError;
        }

        DayOfWeek? dayOfWeek = null;
        if (!string.IsNullOrWhiteSpace(settings.DayOfWeek))
        {
            if (Enum.TryParse<DayOfWeek>(settings.DayOfWeek, true, out var dow))
                dayOfWeek = dow;
        }

        var schedule = await _scheduledReportService.CreateScheduledReportAsync(
            settings.TenantId,
            settings.Name,
            format,
            scheduleType,
            scheduledTime,
            settings.OutputDir,
            settings.Description,
            dayOfWeek,
            settings.DayOfMonth,
            null, // searchCriteriaJson
            null, // customMarketId
            settings.FilenameTemplate,
            settings.EmailRecipients,
            settings.IncludeStats,
            settings.IncludeTrends,
            settings.MaxListings,
            settings.RetentionCount
        );

        AnsiConsole.MarkupLine($"[green]Created scheduled report '{schedule.Name}' ({schedule.ScheduledReportId.Value})[/]");
        if (schedule.NextRunAt.HasValue)
        {
            AnsiConsole.MarkupLine($"[dim]Next run: {schedule.NextRunAt.Value:yyyy-MM-dd HH:mm:ss} UTC[/]");
        }
        return ExitCodes.Success;
    }

    private async Task<int> UpdateScheduleAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        if (!Enum.TryParse<ReportFormat>(settings.Format ?? schedule.Format.ToString(), true, out var format))
            format = schedule.Format;

        if (!Enum.TryParse<ReportSchedule>(settings.ScheduleType ?? schedule.Schedule.ToString(), true, out var scheduleType))
            scheduleType = schedule.Schedule;

        if (!TimeSpan.TryParse(settings.Time ?? schedule.ScheduledTime.ToString(@"hh\:mm"), out var scheduledTime))
            scheduledTime = schedule.ScheduledTime;

        DayOfWeek? dayOfWeek = schedule.ScheduledDayOfWeek;
        if (!string.IsNullOrWhiteSpace(settings.DayOfWeek))
        {
            if (Enum.TryParse<DayOfWeek>(settings.DayOfWeek, true, out var dow))
                dayOfWeek = dow;
        }

        var updated = await _scheduledReportService.UpdateScheduledReportAsync(
            settings.TenantId,
            schedule.ScheduledReportId,
            settings.NewName ?? schedule.Name,
            format,
            scheduleType,
            scheduledTime,
            settings.OutputDir ?? schedule.OutputDirectory,
            settings.Description ?? schedule.Description,
            dayOfWeek,
            settings.DayOfMonth ?? schedule.ScheduledDayOfMonth,
            schedule.SearchCriteriaJson,
            schedule.CustomMarketId,
            settings.FilenameTemplate ?? schedule.FilenameTemplate,
            settings.EmailRecipients ?? schedule.EmailRecipients
        );

        AnsiConsole.MarkupLine($"[green]Updated scheduled report '{updated.Name}'[/]");
        return ExitCodes.Success;
    }

    private async Task<int> DeleteScheduleAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        if (!settings.Force)
        {
            if (!AnsiConsole.Confirm($"Delete schedule '{schedule.Name}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                return ExitCodes.Success;
            }
        }

        var success = await _scheduledReportService.DeleteScheduledReportAsync(settings.TenantId, schedule.ScheduledReportId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Deleted scheduled report '{schedule.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to delete schedule[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> PauseScheduleAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        var success = await _scheduledReportService.PauseScheduledReportAsync(settings.TenantId, schedule.ScheduledReportId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[yellow]Paused scheduled report '{schedule.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to pause schedule[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> ResumeScheduleAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        var success = await _scheduledReportService.ResumeScheduledReportAsync(settings.TenantId, schedule.ScheduledReportId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Resumed scheduled report '{schedule.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to resume schedule[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> CancelScheduleAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        var success = await _scheduledReportService.CancelScheduledReportAsync(settings.TenantId, schedule.ScheduledReportId);
        if (success)
        {
            AnsiConsole.MarkupLine($"[dim]Cancelled scheduled report '{schedule.Name}'[/]");
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine("[red]Failed to cancel schedule[/]");
        return ExitCodes.GeneralError;
    }

    private async Task<int> RunNowAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        AnsiConsole.Status()
            .Start($"Running report '{schedule.Name}'...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        try
        {
            var report = await _scheduledReportService.RunNowAsync(settings.TenantId, schedule.ScheduledReportId);
            AnsiConsole.MarkupLine($"[green]Report generated successfully![/]");
            AnsiConsole.MarkupLine($"[dim]File: {report.FilePath}[/]");
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to generate report: {ex.Message}[/]");
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> ShowScheduleAsync(Settings settings)
    {
        var schedule = await FindScheduleAsync(settings);
        if (schedule == null) return ExitCodes.NotFound;

        var statusColor = schedule.Status switch
        {
            ScheduledReportStatus.Active => "green",
            ScheduledReportStatus.Paused => "yellow",
            ScheduledReportStatus.Completed => "blue",
            ScheduledReportStatus.Cancelled => "dim",
            ScheduledReportStatus.Error => "red",
            _ => "white"
        };

        var panel = new Panel($"[bold]Scheduled Report: {schedule.Name}[/]")
        {
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Property[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("ID", schedule.ScheduledReportId.Value.ToString());
        table.AddRow("Name", schedule.Name);
        table.AddRow("Description", schedule.Description ?? "-");
        table.AddRow("Format", schedule.Format.ToString());
        table.AddRow("Schedule", schedule.Schedule.ToString());
        table.AddRow("Time", schedule.ScheduledTime.ToString(@"hh\:mm") + " UTC");
        if (schedule.ScheduledDayOfWeek.HasValue)
            table.AddRow("Day of Week", schedule.ScheduledDayOfWeek.Value.ToString());
        if (schedule.ScheduledDayOfMonth.HasValue)
            table.AddRow("Day of Month", schedule.ScheduledDayOfMonth.Value.ToString());
        table.AddRow("Status", $"[{statusColor}]{schedule.Status}[/]");
        table.AddRow("Next Run", schedule.NextRunAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
        table.AddRow("Last Run", schedule.LastRunAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");
        table.AddRow("Successful Runs", schedule.SuccessfulRuns.ToString());
        table.AddRow("Failed Runs", schedule.FailedRuns.ToString());
        table.AddRow("Output Directory", schedule.OutputDirectory);
        table.AddRow("Max Listings", schedule.MaxListings.ToString());
        table.AddRow("Retention Count", schedule.RetentionCount.ToString());

        if (!string.IsNullOrWhiteSpace(schedule.LastErrorMessage))
        {
            table.AddRow("[red]Last Error[/]", schedule.LastErrorMessage);
        }

        AnsiConsole.Write(table);
        return ExitCodes.Success;
    }

    private async Task<ScheduledReport?> FindScheduleAsync(Settings settings)
    {
        if (settings.ScheduleId.HasValue)
        {
            var schedule = await _scheduledReportService.GetScheduledReportAsync(
                settings.TenantId,
                new ScheduledReportId(settings.ScheduleId.Value));
            if (schedule == null)
            {
                AnsiConsole.MarkupLine($"[red]Schedule with ID '{settings.ScheduleId}' not found[/]");
            }
            return schedule;
        }

        if (!string.IsNullOrWhiteSpace(settings.Name))
        {
            var schedules = await _scheduledReportService.GetAllScheduledReportsAsync(settings.TenantId);
            var schedule = schedules.FirstOrDefault(s => s.Name.Equals(settings.Name, StringComparison.OrdinalIgnoreCase));
            if (schedule == null)
            {
                AnsiConsole.MarkupLine($"[red]Schedule '{settings.Name}' not found[/]");
            }
            return schedule;
        }

        AnsiConsole.MarkupLine("[red]Error: --name or --schedule-id is required[/]");
        return null;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[subcommand]")]
        [Description("Subcommand: list (default), create, update, delete, pause, resume, cancel, run, show")]
        public string? SubCommand { get; set; }

        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("--schedule-id <GUID>")]
        [Description("Schedule ID")]
        public Guid? ScheduleId { get; set; }

        [CommandOption("-n|--name <NAME>")]
        [Description("Schedule name")]
        public string? Name { get; set; }

        [CommandOption("--new-name <NAME>")]
        [Description("New name for update command")]
        public string? NewName { get; set; }

        [CommandOption("-d|--description <TEXT>")]
        [Description("Schedule description")]
        public string? Description { get; set; }

        [CommandOption("-f|--format <FORMAT>")]
        [Description("Report format (html, pdf, excel)")]
        [DefaultValue("pdf")]
        public string? Format { get; set; } = "pdf";

        [CommandOption("-s|--schedule <TYPE>")]
        [Description("Schedule type (once, daily, weekly, biweekly, monthly, quarterly)")]
        [DefaultValue("daily")]
        public string? ScheduleType { get; set; } = "daily";

        [CommandOption("--time <TIME>")]
        [Description("Scheduled time in HH:MM format (UTC)")]
        [DefaultValue("06:00")]
        public string? Time { get; set; } = "06:00";

        [CommandOption("--day-of-week <DAY>")]
        [Description("Day of week for weekly schedules")]
        public string? DayOfWeek { get; set; }

        [CommandOption("--day-of-month <DAY>")]
        [Description("Day of month for monthly schedules (1-28)")]
        public int? DayOfMonth { get; set; }

        [CommandOption("-o|--output-dir <PATH>")]
        [Description("Output directory for reports")]
        public string? OutputDir { get; set; }

        [CommandOption("--filename-template <TEMPLATE>")]
        [Description("Filename template (e.g., 'report-{date:yyyyMMdd}')")]
        public string? FilenameTemplate { get; set; }

        [CommandOption("--email <ADDRESSES>")]
        [Description("Email recipients (comma-separated)")]
        public string? EmailRecipients { get; set; }

        [CommandOption("--include-stats")]
        [Description("Include statistics in reports")]
        [DefaultValue(true)]
        public bool IncludeStats { get; set; } = true;

        [CommandOption("--include-trends")]
        [Description("Include price trends in reports")]
        [DefaultValue(true)]
        public bool IncludeTrends { get; set; } = true;

        [CommandOption("--max-listings <COUNT>")]
        [Description("Maximum listings per report")]
        [DefaultValue(1000)]
        public int MaxListings { get; set; } = 1000;

        [CommandOption("--retention <COUNT>")]
        [Description("Number of reports to retain")]
        [DefaultValue(10)]
        public int RetentionCount { get; set; } = 10;

        [CommandOption("--status <STATUS>")]
        [Description("Filter by status (active, paused, completed, cancelled, error)")]
        public string? Status { get; set; }

        [CommandOption("--force")]
        [Description("Skip confirmation prompts")]
        public bool Force { get; set; }
    }
}
