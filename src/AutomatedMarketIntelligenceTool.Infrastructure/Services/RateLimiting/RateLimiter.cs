using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

public class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, DomainRateState> _domainStates = new();
    private readonly RateLimitConfiguration _config;
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(RateLimitConfiguration config, ILogger<RateLimiter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task WaitAsync(string domain, CancellationToken cancellationToken = default)
    {
        var state = _domainStates.GetOrAdd(domain, _ => new DomainRateState());

        var delay = state.GetRequiredDelay(_config.DefaultDelayMs);
        if (delay > TimeSpan.Zero)
        {
            _logger.LogDebug("Rate limiting: waiting {Delay}ms for {Domain}",
                delay.TotalMilliseconds, domain);
            await Task.Delay(delay, cancellationToken);
        }

        state.RecordRequest();
    }

    public void ReportRateLimitHit(string domain)
    {
        var state = _domainStates.GetOrAdd(domain, _ => new DomainRateState());
        state.IncreaseBackoff();
        _logger.LogWarning("Rate limit hit for {Domain}, increasing backoff", domain);
    }

    public RateLimitStatus GetStatus(string domain)
    {
        if (_domainStates.TryGetValue(domain, out var state))
        {
            return state.GetStatus(domain, _config.DefaultDelayMs);
        }

        return new RateLimitStatus
        {
            Domain = domain,
            RequestCount = 0,
            CurrentDelayMs = _config.DefaultDelayMs,
            BackoffLevel = 0
        };
    }
}
