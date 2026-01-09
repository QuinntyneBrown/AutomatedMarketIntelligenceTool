namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;

/// <summary>
/// Represents the health status of a scraper.
/// </summary>
public enum ScraperHealthStatus
{
    /// <summary>
    /// Scraper is operating normally with good success rate.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Scraper is functioning but with reduced effectiveness (lower success rate, missing elements, or zero results).
    /// </summary>
    Degraded,
    
    /// <summary>
    /// Scraper is not functioning properly (high failure rate).
    /// </summary>
    Failed
}
