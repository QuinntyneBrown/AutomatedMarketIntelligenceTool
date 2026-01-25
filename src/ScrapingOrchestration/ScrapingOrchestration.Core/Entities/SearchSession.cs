using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;

namespace ScrapingOrchestration.Core.Entities;

/// <summary>
/// Represents a search session for scraping operations.
/// </summary>
public sealed class SearchSession
{
    public Guid Id { get; init; }
    public SearchParameters Parameters { get; init; } = new();
    public SearchSessionStatus Status { get; private set; }
    public IReadOnlyList<ScrapingSource> Sources { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalListingsFound { get; private set; }
    public int TotalErrors { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? UserId { get; init; }

    private SearchSession() { }

    public static SearchSession Create(
        SearchParameters parameters,
        IEnumerable<ScrapingSource> sources,
        Guid? userId = null)
    {
        return new SearchSession
        {
            Id = Guid.NewGuid(),
            Parameters = parameters,
            Sources = sources.ToList(),
            Status = SearchSessionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UserId = userId
        };
    }

    public void Start()
    {
        if (Status != SearchSessionStatus.Pending)
            throw new InvalidOperationException($"Cannot start session in {Status} status");

        Status = SearchSessionStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(int listingsFound)
    {
        if (Status != SearchSessionStatus.Running)
            throw new InvalidOperationException($"Cannot complete session in {Status} status");

        Status = SearchSessionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        TotalListingsFound = listingsFound;
    }

    public void Fail(string errorMessage, int errorsCount = 1)
    {
        if (Status != SearchSessionStatus.Running && Status != SearchSessionStatus.Pending)
            throw new InvalidOperationException($"Cannot fail session in {Status} status");

        Status = SearchSessionStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
        TotalErrors = errorsCount;
    }

    public void Cancel()
    {
        if (Status == SearchSessionStatus.Completed || Status == SearchSessionStatus.Failed)
            throw new InvalidOperationException($"Cannot cancel session in {Status} status");

        Status = SearchSessionStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Pause()
    {
        if (Status != SearchSessionStatus.Running)
            throw new InvalidOperationException($"Cannot pause session in {Status} status");

        Status = SearchSessionStatus.Paused;
    }

    public void Resume()
    {
        if (Status != SearchSessionStatus.Paused)
            throw new InvalidOperationException($"Cannot resume session in {Status} status");

        Status = SearchSessionStatus.Running;
    }

    public void IncrementListings(int count)
    {
        TotalListingsFound += count;
    }

    public void IncrementErrors(int count = 1)
    {
        TotalErrors += count;
    }
}
