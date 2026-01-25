using Dashboard.Core.Models;

namespace Dashboard.Core.Interfaces;

/// <summary>
/// Service interface for dashboard operations.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the dashboard overview with key metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard overview data.</returns>
    Task<DashboardOverview> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets market trends and analytics.
    /// </summary>
    /// <param name="make">Optional filter by make.</param>
    /// <param name="province">Optional filter by province.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Market trends data.</returns>
    Task<MarketTrends> GetMarketTrendsAsync(
        string? make = null,
        string? province = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current scraping status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scraping status data.</returns>
    Task<ScrapingStatus> GetScrapingStatusAsync(CancellationToken cancellationToken = default);
}
