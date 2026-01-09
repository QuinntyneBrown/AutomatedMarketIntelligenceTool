namespace AutomatedMarketIntelligenceTool.Core.Services.Dashboard;

/// <summary>
/// Complete dashboard data including all metrics and trends.
/// </summary>
public class DashboardData
{
    /// <summary>
    /// Summary of listing counts and activity.
    /// </summary>
    public ListingSummary ListingSummary { get; set; } = new();

    /// <summary>
    /// Summary of watch list items.
    /// </summary>
    public WatchListSummary WatchListSummary { get; set; } = new();

    /// <summary>
    /// Summary of active alerts and recent notifications.
    /// </summary>
    public AlertSummary AlertSummary { get; set; } = new();

    /// <summary>
    /// Market trend data over time.
    /// </summary>
    public MarketTrends MarketTrends { get; set; } = new();

    /// <summary>
    /// System health and performance metrics.
    /// </summary>
    public SystemMetrics SystemMetrics { get; set; } = new();

    /// <summary>
    /// Timestamp when this dashboard data was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Summary of listings in the system.
/// </summary>
public class ListingSummary
{
    /// <summary>
    /// Total number of listings in the database.
    /// </summary>
    public int TotalListings { get; set; }

    /// <summary>
    /// Number of active listings.
    /// </summary>
    public int ActiveListings { get; set; }

    /// <summary>
    /// Number of new listings found today.
    /// </summary>
    public int NewToday { get; set; }

    /// <summary>
    /// Number of new listings in the last 7 days.
    /// </summary>
    public int NewThisWeek { get; set; }

    /// <summary>
    /// Number of price drops today.
    /// </summary>
    public int PriceDropsToday { get; set; }

    /// <summary>
    /// Number of price increases today.
    /// </summary>
    public int PriceIncreasesToday { get; set; }

    /// <summary>
    /// Number of listings deactivated today.
    /// </summary>
    public int DeactivatedToday { get; set; }

    /// <summary>
    /// Total number of unique vehicles tracked.
    /// </summary>
    public int UniqueVehicles { get; set; }

    /// <summary>
    /// Breakdown of listings by source site.
    /// </summary>
    public Dictionary<string, int> BySource { get; set; } = new();
}

/// <summary>
/// Summary of watch list activity.
/// </summary>
public class WatchListSummary
{
    /// <summary>
    /// Total number of watched listings.
    /// </summary>
    public int TotalWatched { get; set; }

    /// <summary>
    /// Number of watched listings with price changes.
    /// </summary>
    public int WithPriceChanges { get; set; }

    /// <summary>
    /// Number of watched listings that are no longer active.
    /// </summary>
    public int NoLongerActive { get; set; }

    /// <summary>
    /// Recently changed watched listings (top 5).
    /// </summary>
    public List<WatchedListingChange> RecentChanges { get; set; } = new();
}

/// <summary>
/// Represents a change to a watched listing.
/// </summary>
public class WatchedListingChange
{
    public Guid ListingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Summary of alerts and notifications.
/// </summary>
public class AlertSummary
{
    /// <summary>
    /// Total number of configured alerts.
    /// </summary>
    public int TotalAlerts { get; set; }

    /// <summary>
    /// Number of active alerts.
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// Number of alerts triggered today.
    /// </summary>
    public int TriggeredToday { get; set; }

    /// <summary>
    /// Number of alerts triggered this week.
    /// </summary>
    public int TriggeredThisWeek { get; set; }

    /// <summary>
    /// Recent alert notifications (top 5).
    /// </summary>
    public List<RecentAlertNotification> RecentNotifications { get; set; } = new();
}

/// <summary>
/// Represents a recent alert notification.
/// </summary>
public class RecentAlertNotification
{
    public Guid AlertId { get; set; }
    public string AlertName { get; set; } = string.Empty;
    public Guid ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
}

/// <summary>
/// Market trend data over configurable time periods.
/// </summary>
public class MarketTrends
{
    /// <summary>
    /// Number of days analyzed for trends.
    /// </summary>
    public int TrendDays { get; set; } = 30;

    /// <summary>
    /// Average price trend over time.
    /// </summary>
    public TrendData AveragePriceTrend { get; set; } = new();

    /// <summary>
    /// Inventory (listing count) trend over time.
    /// </summary>
    public TrendData InventoryTrend { get; set; } = new();

    /// <summary>
    /// New listings rate trend over time.
    /// </summary>
    public TrendData NewListingsRateTrend { get; set; } = new();

    /// <summary>
    /// Average days on market trend.
    /// </summary>
    public TrendData DaysOnMarketTrend { get; set; } = new();

    /// <summary>
    /// Daily breakdown of new listings.
    /// </summary>
    public List<DailyMetric> DailyNewListings { get; set; } = new();

    /// <summary>
    /// Daily breakdown of price changes.
    /// </summary>
    public List<DailyMetric> DailyPriceChanges { get; set; } = new();
}

/// <summary>
/// Represents a trend with current value and change.
/// </summary>
public class TrendData
{
    /// <summary>
    /// Current value of the metric.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Previous value for comparison (e.g., last period).
    /// </summary>
    public decimal PreviousValue { get; set; }

    /// <summary>
    /// Absolute change (CurrentValue - PreviousValue).
    /// </summary>
    public decimal Change => CurrentValue - PreviousValue;

    /// <summary>
    /// Percentage change from previous value.
    /// </summary>
    public decimal PercentageChange => PreviousValue != 0
        ? Math.Round((Change / PreviousValue) * 100, 2)
        : 0;

    /// <summary>
    /// Trend direction indicator.
    /// </summary>
    public TrendDirection Direction => Change switch
    {
        > 0 => TrendDirection.Up,
        < 0 => TrendDirection.Down,
        _ => TrendDirection.Stable
    };
}

/// <summary>
/// Trend direction enumeration.
/// </summary>
public enum TrendDirection
{
    Up,
    Down,
    Stable
}

/// <summary>
/// Daily metric data point.
/// </summary>
public class DailyMetric
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal? AverageValue { get; set; }
}

/// <summary>
/// System health and performance metrics.
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// Total number of search sessions.
    /// </summary>
    public int TotalSearchSessions { get; set; }

    /// <summary>
    /// Search sessions in the last 24 hours.
    /// </summary>
    public int SearchesLast24Hours { get; set; }

    /// <summary>
    /// Number of saved search profiles.
    /// </summary>
    public int SavedProfiles { get; set; }

    /// <summary>
    /// Database size summary.
    /// </summary>
    public DatabaseStats DatabaseStats { get; set; } = new();

    /// <summary>
    /// Scraper health status by site.
    /// </summary>
    public Dictionary<string, ScraperStatus> ScraperHealth { get; set; } = new();
}

/// <summary>
/// Database statistics.
/// </summary>
public class DatabaseStats
{
    public int TotalListings { get; set; }
    public int TotalPriceHistoryRecords { get; set; }
    public int TotalVehicles { get; set; }
    public int TotalSearchSessions { get; set; }
}

/// <summary>
/// Scraper status for a specific site.
/// </summary>
public class ScraperStatus
{
    public string SiteName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public double SuccessRate { get; set; }
    public int ListingsFound { get; set; }
    public DateTime? LastRun { get; set; }
}
