namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

public interface IRateLimiter
{
    Task WaitAsync(string domain, CancellationToken cancellationToken = default);
    void ReportRateLimitHit(string domain);
    RateLimitStatus GetStatus(string domain);
}
