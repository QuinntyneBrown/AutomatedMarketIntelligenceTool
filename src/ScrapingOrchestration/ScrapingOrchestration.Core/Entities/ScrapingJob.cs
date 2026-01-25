using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;

namespace ScrapingOrchestration.Core.Entities;

/// <summary>
/// Represents a single scraping job for a specific source.
/// </summary>
public sealed class ScrapingJob
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public ScrapingSource Source { get; init; }
    public SearchParameters Parameters { get; init; } = new();
    public ScrapingJobStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int ListingsFound { get; private set; }
    public int PagesCrawled { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public string? WorkerId { get; private set; }

    private ScrapingJob() { }

    public static ScrapingJob Create(
        Guid sessionId,
        ScrapingSource source,
        SearchParameters parameters)
    {
        return new ScrapingJob
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Source = source,
            Parameters = parameters,
            Status = ScrapingJobStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Start(string workerId)
    {
        if (Status != ScrapingJobStatus.Pending && Status != ScrapingJobStatus.Retry)
            throw new InvalidOperationException($"Cannot start job in {Status} status");

        Status = ScrapingJobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
        WorkerId = workerId;
    }

    public void Complete(int listingsFound, int pagesCrawled)
    {
        if (Status != ScrapingJobStatus.Running)
            throw new InvalidOperationException($"Cannot complete job in {Status} status");

        Status = ScrapingJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        ListingsFound = listingsFound;
        PagesCrawled = pagesCrawled;
    }

    public void Fail(string errorMessage)
    {
        if (Status != ScrapingJobStatus.Running)
            throw new InvalidOperationException($"Cannot fail job in {Status} status");

        Status = ScrapingJobStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void ScheduleRetry(int maxRetries = 3)
    {
        if (Status != ScrapingJobStatus.Failed)
            throw new InvalidOperationException($"Cannot retry job in {Status} status");

        if (RetryCount >= maxRetries)
            return;

        Status = ScrapingJobStatus.Retry;
        RetryCount++;
        StartedAt = null;
        CompletedAt = null;
        WorkerId = null;
    }

    public void UpdateProgress(int listingsFound, int pagesCrawled)
    {
        ListingsFound = listingsFound;
        PagesCrawled = pagesCrawled;
    }
}

/// <summary>
/// Status of a scraping job.
/// </summary>
public enum ScrapingJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Retry = 4
}
