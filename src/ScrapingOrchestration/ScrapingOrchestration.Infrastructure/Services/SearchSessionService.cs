using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrapingOrchestration.Core.Entities;
using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.Events;
using ScrapingOrchestration.Core.Services;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingOrchestration.Infrastructure.Data;
using Shared.Messaging;

namespace ScrapingOrchestration.Infrastructure.Services;

/// <summary>
/// Service for managing search sessions.
/// </summary>
public sealed class SearchSessionService : ISearchSessionService
{
    private readonly ScrapingOrchestrationDbContext _dbContext;
    private readonly IJobScheduler _jobScheduler;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<SearchSessionService> _logger;

    public SearchSessionService(
        ScrapingOrchestrationDbContext dbContext,
        IJobScheduler jobScheduler,
        IEventPublisher eventPublisher,
        ILogger<SearchSessionService> logger)
    {
        _dbContext = dbContext;
        _jobScheduler = jobScheduler;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SearchSession> CreateSessionAsync(
        SearchParameters parameters,
        IEnumerable<ScrapingSource> sources,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var sourcesList = sources.ToList();
        var session = SearchSession.Create(parameters, sourcesList, userId);

        _dbContext.Sessions.Add(session);

        // Create jobs for each source
        var jobs = sourcesList
            .Select(source => ScrapingJob.Create(session.Id, source, parameters))
            .ToList();

        _dbContext.Jobs.AddRange(jobs);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Schedule the jobs
        await _jobScheduler.ScheduleJobsAsync(jobs, cancellationToken);

        // Publish events for each job
        foreach (var job in jobs)
        {
            await _eventPublisher.PublishAsync(new ScrapingJobCreatedEvent
            {
                JobId = job.Id,
                SessionId = session.Id,
                Source = job.Source,
                Parameters = parameters
            }, cancellationToken);
        }

        _logger.LogInformation(
            "Created session {SessionId} with {JobCount} jobs for sources: {Sources}",
            session.Id, jobs.Count, string.Join(", ", sourcesList));

        return session;
    }

    /// <inheritdoc />
    public async Task<SearchSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions.FindAsync([sessionId], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchSession>> GetSessionsByStatusAsync(SearchSessionStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateSessionAsync(SearchSession session, CancellationToken cancellationToken = default)
    {
        _dbContext.Sessions.Update(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for cancellation", sessionId);
            return;
        }

        session.Cancel();
        await UpdateSessionAsync(session, cancellationToken);

        // Cancel all pending/running jobs
        var jobs = await _dbContext.Jobs
            .Where(j => j.SessionId == sessionId &&
                        (j.Status == ScrapingJobStatus.Pending || j.Status == ScrapingJobStatus.Running))
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            await _jobScheduler.CancelJobAsync(job.Id, cancellationToken);
        }

        _logger.LogInformation("Cancelled session {SessionId} with {JobCount} jobs", sessionId, jobs.Count);
    }
}
