namespace AutomatedMarketIntelligenceTool.Core.Services.Dashboard;

/// <summary>
/// Service for aggregating and providing dashboard data.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets complete dashboard data including all metrics and trends.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenancy support.</param>
    /// <param name="trendDays">Number of days to calculate trends for (default: 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete dashboard data.</returns>
    Task<DashboardData> GetDashboardDataAsync(
        Guid tenantId,
        int trendDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of listings in the system.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenancy support.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Listing summary data.</returns>
    Task<ListingSummary> GetListingSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of watch list activity.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenancy support.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Watch list summary data.</returns>
    Task<WatchListSummary> GetWatchListSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of alerts and notifications.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenancy support.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert summary data.</returns>
    Task<AlertSummary> GetAlertSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets market trend data over a configurable time period.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenancy support.</param>
    /// <param name="trendDays">Number of days to calculate trends for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Market trend data.</returns>
    Task<MarketTrends> GetMarketTrendsAsync(
        Guid tenantId,
        int trendDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system health and performance metrics.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenancy support.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System metrics data.</returns>
    Task<SystemMetrics> GetSystemMetricsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
