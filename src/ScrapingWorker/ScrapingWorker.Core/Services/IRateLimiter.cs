namespace ScrapingWorker.Core.Services;

/// <summary>
/// Interface for rate limiting requests.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Waits until a request can be made without exceeding the rate limit.
    /// </summary>
    Task WaitAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a request for rate limiting purposes.
    /// </summary>
    void RecordRequest(string key);

    /// <summary>
    /// Checks if a request can be made without waiting.
    /// </summary>
    bool CanMakeRequest(string key);
}
