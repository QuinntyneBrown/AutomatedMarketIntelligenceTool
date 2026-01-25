using Microsoft.Extensions.Logging;

namespace Shared.Messaging.Retry;

/// <summary>
/// Retry policy for message handling with exponential backoff.
/// </summary>
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly ILogger _logger;

    public RetryPolicy(int maxRetries, TimeSpan initialDelay, ILogger logger)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay;
        _logger = logger;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = _initialDelay;

        while (true)
        {
            try
            {
                attempt++;
                return await action();
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                _logger.LogWarning(ex,
                    "Attempt {Attempt} of {MaxRetries} failed. Retrying in {Delay}ms",
                    attempt, _maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }
    }

    public async Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }
}
