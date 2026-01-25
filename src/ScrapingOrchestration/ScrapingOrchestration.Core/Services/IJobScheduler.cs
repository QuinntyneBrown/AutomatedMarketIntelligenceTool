using ScrapingOrchestration.Core.Entities;

namespace ScrapingOrchestration.Core.Services;

/// <summary>
/// Interface for scheduling scraping jobs.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Schedules a scraping job for execution.
    /// </summary>
    Task ScheduleJobAsync(ScrapingJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules multiple scraping jobs for execution.
    /// </summary>
    Task ScheduleJobsAsync(IEnumerable<ScrapingJob> jobs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled job.
    /// </summary>
    Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next pending job from the queue.
    /// </summary>
    Task<ScrapingJob?> GetNextJobAsync(CancellationToken cancellationToken = default);
}
