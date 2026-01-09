using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Scheduling;

/// <summary>
/// Background service that processes scheduled reports.
/// </summary>
public class ReportSchedulerService : IReportSchedulerService
{
    private readonly IScheduledReportService _scheduledReportService;
    private readonly ILogger<ReportSchedulerService> _logger;
    private readonly TimeSpan _checkInterval;

    public ReportSchedulerService(
        IScheduledReportService scheduledReportService,
        ILogger<ReportSchedulerService> logger,
        TimeSpan? checkInterval = null)
    {
        _scheduledReportService = scheduledReportService ?? throw new ArgumentNullException(nameof(scheduledReportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    public TimeSpan CheckInterval => _checkInterval;

    public async Task ProcessDueReportsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for due scheduled reports");

        try
        {
            var dueReports = await _scheduledReportService.GetDueScheduledReportsAsync(cancellationToken);

            if (dueReports.Count == 0)
            {
                _logger.LogDebug("No scheduled reports due at this time");
                return;
            }

            _logger.LogInformation("Found {Count} scheduled reports due for processing", dueReports.Count);

            foreach (var scheduledReport in dueReports)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation(
                        "Processing scheduled report {ScheduledReportId} '{Name}'",
                        scheduledReport.ScheduledReportId.Value, scheduledReport.Name);

                    await _scheduledReportService.RunNowAsync(
                        scheduledReport.TenantId,
                        scheduledReport.ScheduledReportId,
                        cancellationToken);

                    _logger.LogInformation(
                        "Successfully processed scheduled report {ScheduledReportId} '{Name}'",
                        scheduledReport.ScheduledReportId.Value, scheduledReport.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process scheduled report {ScheduledReportId} '{Name}'",
                        scheduledReport.ScheduledReportId.Value, scheduledReport.Name);
                    // Continue processing other reports
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking for due scheduled reports");
        }
    }

    public async Task RunSchedulerLoopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Report scheduler started with check interval of {Interval}", _checkInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            await ProcessDueReportsAsync(cancellationToken);

            try
            {
                await Task.Delay(_checkInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Report scheduler stopped");
    }
}

/// <summary>
/// Interface for the report scheduler service.
/// </summary>
public interface IReportSchedulerService
{
    /// <summary>
    /// Gets the interval at which the scheduler checks for due reports.
    /// </summary>
    TimeSpan CheckInterval { get; }

    /// <summary>
    /// Processes all due scheduled reports.
    /// </summary>
    Task ProcessDueReportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the scheduler loop that continuously checks for and processes due reports.
    /// </summary>
    Task RunSchedulerLoopAsync(CancellationToken cancellationToken = default);
}
