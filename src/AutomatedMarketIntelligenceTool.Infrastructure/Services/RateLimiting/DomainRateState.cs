namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

internal class DomainRateState
{
    private DateTime _lastRequestTime;
    private int _backoffLevel;
    private int _requestCount;
    private readonly object _lock = new();

    public DomainRateState()
    {
        _lastRequestTime = DateTime.MinValue;
        _backoffLevel = 0;
        _requestCount = 0;
    }

    public TimeSpan GetRequiredDelay(int defaultDelayMs)
    {
        lock (_lock)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var requiredDelay = TimeSpan.FromMilliseconds(defaultDelayMs * Math.Pow(2, _backoffLevel));
            var remainingDelay = requiredDelay - timeSinceLastRequest;

            return remainingDelay > TimeSpan.Zero ? remainingDelay : TimeSpan.Zero;
        }
    }

    public void RecordRequest()
    {
        lock (_lock)
        {
            _lastRequestTime = DateTime.UtcNow;
            _requestCount++;
        }
    }

    public void IncreaseBackoff()
    {
        lock (_lock)
        {
            _backoffLevel++;
        }
    }

    public RateLimitStatus GetStatus(string domain, int defaultDelayMs)
    {
        lock (_lock)
        {
            return new RateLimitStatus
            {
                Domain = domain,
                RequestCount = _requestCount,
                LastRequestTime = _lastRequestTime != DateTime.MinValue ? _lastRequestTime : null,
                CurrentDelayMs = (int)(defaultDelayMs * Math.Pow(2, _backoffLevel)),
                BackoffLevel = _backoffLevel
            };
        }
    }

    public void ResetBackoff()
    {
        lock (_lock)
        {
            _backoffLevel = 0;
        }
    }
}
