using Microsoft.Extensions.Logging;
using ScrapingOrchestration.Core.Entities;
using ScrapingOrchestration.Core.Services;
using System.Collections.Concurrent;

namespace ScrapingOrchestration.Infrastructure.Services;

/// <summary>
/// In-memory job scheduler for development and testing.
/// For production, use a distributed queue like RabbitMQ or Redis.
/// </summary>
public sealed class InMemoryJobScheduler : IJobScheduler
{
    private readonly ConcurrentQueue<ScrapingJob> _jobQueue = new();
    private readonly ConcurrentDictionary<Guid, ScrapingJob> _scheduledJobs = new();
    private readonly ILogger<InMemoryJobScheduler> _logger;

    public InMemoryJobScheduler(ILogger<InMemoryJobScheduler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ScheduleJobAsync(ScrapingJob job, CancellationToken cancellationToken = default)
    {
        _scheduledJobs[job.Id] = job;
        _jobQueue.Enqueue(job);
        _logger.LogDebug("Scheduled job {JobId} for source {Source}", job.Id, job.Source);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ScheduleJobsAsync(IEnumerable<ScrapingJob> jobs, CancellationToken cancellationToken = default)
    {
        foreach (var job in jobs)
        {
            _scheduledJobs[job.Id] = job;
            _jobQueue.Enqueue(job);
        }
        _logger.LogDebug("Scheduled {Count} jobs", _jobQueue.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (_scheduledJobs.TryRemove(jobId, out var job))
        {
            _logger.LogDebug("Cancelled job {JobId}", jobId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ScrapingJob?> GetNextJobAsync(CancellationToken cancellationToken = default)
    {
        while (_jobQueue.TryDequeue(out var job))
        {
            // Check if job is still scheduled (not cancelled)
            if (_scheduledJobs.TryRemove(job.Id, out _))
            {
                return Task.FromResult<ScrapingJob?>(job);
            }
        }
        return Task.FromResult<ScrapingJob?>(null);
    }
}
