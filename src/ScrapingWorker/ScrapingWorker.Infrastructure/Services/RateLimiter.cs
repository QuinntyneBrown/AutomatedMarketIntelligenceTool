using ScrapingWorker.Core.Services;
using System.Collections.Concurrent;

namespace ScrapingWorker.Infrastructure.Services;

/// <summary>
/// Token bucket rate limiter implementation.
/// </summary>
public sealed class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();
    private readonly int _requestsPerMinute;
    private readonly TimeSpan _minInterval;

    public RateLimiter(int requestsPerMinute = 30)
    {
        _requestsPerMinute = requestsPerMinute;
        _minInterval = TimeSpan.FromMilliseconds(60000.0 / requestsPerMinute);
    }

    /// <inheritdoc />
    public async Task WaitAsync(string key, CancellationToken cancellationToken = default)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new RateLimitBucket(_requestsPerMinute));

        while (!bucket.TryConsume())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(_minInterval, cancellationToken);
            bucket.Refill();
        }
    }

    /// <inheritdoc />
    public void RecordRequest(string key)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new RateLimitBucket(_requestsPerMinute));
        bucket.TryConsume();
    }

    /// <inheritdoc />
    public bool CanMakeRequest(string key)
    {
        if (!_buckets.TryGetValue(key, out var bucket))
            return true;

        bucket.Refill();
        return bucket.HasTokens;
    }

    private sealed class RateLimitBucket
    {
        private readonly int _maxTokens;
        private int _tokens;
        private DateTime _lastRefill;

        public RateLimitBucket(int maxTokens)
        {
            _maxTokens = maxTokens;
            _tokens = maxTokens;
            _lastRefill = DateTime.UtcNow;
        }

        public bool HasTokens => _tokens > 0;

        public bool TryConsume()
        {
            Refill();
            if (_tokens > 0)
            {
                _tokens--;
                return true;
            }
            return false;
        }

        public void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastRefill;

            if (elapsed.TotalMinutes >= 1)
            {
                _tokens = _maxTokens;
                _lastRefill = now;
            }
            else
            {
                var tokensToAdd = (int)(elapsed.TotalMinutes * _maxTokens);
                if (tokensToAdd > 0)
                {
                    _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
                    _lastRefill = now;
                }
            }
        }
    }
}
