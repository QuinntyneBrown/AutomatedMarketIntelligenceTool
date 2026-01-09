namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

public class RateLimitConfiguration
{
    public int DefaultDelayMs { get; set; } = 3000;
    public int MaxBackoffMs { get; set; } = 30000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int BackoffResetMinutes { get; set; } = 15;
}
