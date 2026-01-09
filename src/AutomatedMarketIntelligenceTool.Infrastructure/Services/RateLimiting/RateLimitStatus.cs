namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

public class RateLimitStatus
{
    public string Domain { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public DateTime? LastRequestTime { get; set; }
    public int CurrentDelayMs { get; set; }
    public int BackoffLevel { get; set; }
}
