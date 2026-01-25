namespace Dashboard.Core.Models;

/// <summary>
/// Represents an overview of the dashboard metrics.
/// </summary>
public sealed class DashboardOverview
{
    /// <summary>
    /// Total number of listings in the system.
    /// </summary>
    public int TotalListings { get; init; }

    /// <summary>
    /// Number of active listings.
    /// </summary>
    public int ActiveListings { get; init; }

    /// <summary>
    /// Total number of vehicles tracked.
    /// </summary>
    public int TotalVehicles { get; init; }

    /// <summary>
    /// Number of active alerts.
    /// </summary>
    public int ActiveAlerts { get; init; }

    /// <summary>
    /// Number of pending reviews.
    /// </summary>
    public int PendingReviews { get; init; }

    /// <summary>
    /// Number of new listings today.
    /// </summary>
    public int NewListingsToday { get; init; }

    /// <summary>
    /// Number of price changes today.
    /// </summary>
    public int PriceChangesToday { get; init; }

    /// <summary>
    /// Number of listings removed today.
    /// </summary>
    public int ListingsRemovedToday { get; init; }

    /// <summary>
    /// Timestamp when this overview was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
