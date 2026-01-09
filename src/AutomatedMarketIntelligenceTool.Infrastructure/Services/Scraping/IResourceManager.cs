namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Service for monitoring and managing system resources during scraping operations.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Gets the current resource utilization.
    /// </summary>
    /// <returns>Current resource metrics.</returns>
    ResourceMetrics GetCurrentMetrics();

    /// <summary>
    /// Determines if the system has available resources for another concurrent operation.
    /// </summary>
    /// <param name="currentConcurrency">Current number of concurrent operations.</param>
    /// <param name="maxConcurrency">Maximum allowed concurrent operations.</param>
    /// <returns>True if resources are available, false otherwise.</returns>
    bool HasAvailableResources(int currentConcurrency, int maxConcurrency);

    /// <summary>
    /// Calculates the recommended concurrency level based on current resource utilization.
    /// </summary>
    /// <param name="requestedConcurrency">Requested concurrency level.</param>
    /// <returns>Adjusted concurrency level based on available resources.</returns>
    int GetRecommendedConcurrency(int requestedConcurrency);
}
