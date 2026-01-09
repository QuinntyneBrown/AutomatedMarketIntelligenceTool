namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

internal class DomainRateState
{
    private DateTime _lastRequestTime;
    private int _backoffLevel;
    private int _requestCount;
    private readonly object _lock = new();
    private const int MaxBackoffLevel = 10; // Prevents overflow and excessive delays

    public DomainRateState()
    {
        _lastRequestTime = DateTime.MinValue;
        _backoffLevel = 0;
        _requestCount = 0;
    }

    public TimeSpan GetRequiredDelay(int defaultDelayMs, int maxBackoffMs = 30000)
    {
        lock (_lock)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var calculatedDelayMs = defaultDelayMs * Math.Pow(2, _backoffLevel);
            var cappedDelayMs = Math.Min(calculatedDelayMs, maxBackoffMs);
            var requiredDelay = TimeSpan.FromMilliseconds(cappedDelayMs);
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
            if (_backoffLevel < MaxBackoffLevel)
            {
                _backoffLevel++;
            }
        }
    }

    public RateLimitStatus GetStatus(string domain, int defaultDelayMs, int maxBackoffMs = 30000)
    {
        lock (_lock)
        {
            var calculatedDelayMs = defaultDelayMs * Math.Pow(2, _backoffLevel);
            var cappedDelayMs = (int)Math.Min(calculatedDelayMs, maxBackoffMs);

            return new RateLimitStatus
            {
                Domain = domain,
                RequestCount = _requestCount,
                LastRequestTime = _lastRequestTime != DateTime.MinValue ? _lastRequestTime : null,
                CurrentDelayMs = cappedDelayMs,
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
