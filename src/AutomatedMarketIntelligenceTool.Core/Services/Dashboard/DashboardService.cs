using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Dashboard;

/// <summary>
/// Service for aggregating and providing dashboard data.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DashboardService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DashboardData> GetDashboardDataAsync(
        Guid tenantId,
        int trendDays = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating dashboard data for tenant {TenantId} with {TrendDays} trend days",
            tenantId, trendDays);

        var dashboard = new DashboardData
        {
            ListingSummary = await GetListingSummaryAsync(tenantId, cancellationToken),
            WatchListSummary = await GetWatchListSummaryAsync(tenantId, cancellationToken),
            AlertSummary = await GetAlertSummaryAsync(tenantId, cancellationToken),
            MarketTrends = await GetMarketTrendsAsync(tenantId, trendDays, cancellationToken),
            SystemMetrics = await GetSystemMetricsAsync(tenantId, cancellationToken),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogDebug("Dashboard data generated successfully with {TotalListings} total listings",
            dashboard.ListingSummary.TotalListings);

        return dashboard;
    }

    /// <inheritdoc />
    public async Task<ListingSummary> GetListingSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        var summary = new ListingSummary
        {
            TotalListings = await _context.Listings.CountAsync(cancellationToken),
            ActiveListings = await _context.Listings.CountAsync(l => l.IsActive, cancellationToken),
            UniqueVehicles = await _context.Vehicles.CountAsync(cancellationToken)
        };

        // New listings today and this week
        summary.NewToday = await _context.Listings
            .CountAsync(l => l.CreatedAt >= today, cancellationToken);

        summary.NewThisWeek = await _context.Listings
            .CountAsync(l => l.CreatedAt >= weekAgo, cancellationToken);

        // Deactivated today
        summary.DeactivatedToday = await _context.Listings
            .CountAsync(l => l.DeactivatedAt.HasValue && l.DeactivatedAt.Value >= today, cancellationToken);

        // Price changes today
        var priceChangesToday = await _context.PriceHistory
            .Where(ph => ph.ObservedAt >= today && ph.PriceChange.HasValue)
            .GroupBy(ph => new { ph.ListingId, IsDecrease = ph.PriceChange < 0 })
            .Select(g => new { g.Key.IsDecrease, Count = g.Count() })
            .ToListAsync(cancellationToken);

        summary.PriceDropsToday = priceChangesToday
            .Where(x => x.IsDecrease)
            .Sum(x => x.Count);

        summary.PriceIncreasesToday = priceChangesToday
            .Where(x => !x.IsDecrease)
            .Sum(x => x.Count);

        // By source breakdown
        summary.BySource = await _context.Listings
            .Where(l => l.IsActive)
            .GroupBy(l => l.SourceSite)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Source, x => x.Count, cancellationToken);

        return summary;
    }

    /// <inheritdoc />
    public async Task<WatchListSummary> GetWatchListSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var summary = new WatchListSummary
        {
            TotalWatched = await _context.WatchedListings.CountAsync(cancellationToken)
        };

        // Get watched listings with their listing details
        var watchedWithListings = await _context.WatchedListings
            .Include(w => w.Listing)
            .ToListAsync(cancellationToken);

        summary.NoLongerActive = watchedWithListings.Count(w => !w.Listing.IsActive);

        // Find watched listings with recent price changes
        var watchedListingIds = watchedWithListings.Select(w => w.ListingId.Value).ToList();
        var recentPriceChanges = await _context.PriceHistory
            .Where(ph => watchedListingIds.Contains(ph.ListingId.Value) && ph.ObservedAt >= today.AddDays(-7))
            .Select(ph => ph.ListingId.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        summary.WithPriceChanges = recentPriceChanges.Count;

        // Get recent changes for watched listings
        var recentChanges = await _context.PriceHistory
            .Where(ph => watchedListingIds.Contains(ph.ListingId.Value) && ph.PriceChange.HasValue)
            .OrderByDescending(ph => ph.ObservedAt)
            .Take(5)
            .Join(_context.Listings,
                ph => ph.ListingId,
                l => l.ListingId,
                (ph, l) => new WatchedListingChange
                {
                    ListingId = l.ListingId.Value,
                    Title = $"{l.Year} {l.Make} {l.Model}",
                    ChangeType = ph.PriceChange < 0 ? "Price Drop" : "Price Increase",
                    Details = $"${ph.Price - ph.PriceChange!.Value:N0} → ${ph.Price:N0}",
                    ChangedAt = ph.ObservedAt
                })
            .ToListAsync(cancellationToken);

        summary.RecentChanges = recentChanges;

        return summary;
    }

    /// <inheritdoc />
    public async Task<AlertSummary> GetAlertSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        var summary = new AlertSummary
        {
            TotalAlerts = await _context.Alerts.CountAsync(cancellationToken),
            ActiveAlerts = await _context.Alerts.CountAsync(a => a.IsActive, cancellationToken)
        };

        // Triggered today and this week
        summary.TriggeredToday = await _context.AlertNotifications
            .CountAsync(n => n.SentAt >= today, cancellationToken);

        summary.TriggeredThisWeek = await _context.AlertNotifications
            .CountAsync(n => n.SentAt >= weekAgo, cancellationToken);

        // Recent notifications
        summary.RecentNotifications = await _context.AlertNotifications
            .Include(n => n.Alert)
            .Include(n => n.Listing)
            .OrderByDescending(n => n.SentAt)
            .Take(5)
            .Select(n => new RecentAlertNotification
            {
                AlertId = n.AlertId.Value,
                AlertName = n.Alert.Name,
                ListingId = n.ListingId.Value,
                ListingTitle = $"{n.Listing.Year} {n.Listing.Make} {n.Listing.Model}",
                TriggeredAt = n.SentAt
            })
            .ToListAsync(cancellationToken);

        return summary;
    }

    /// <inheritdoc />
    public async Task<MarketTrends> GetMarketTrendsAsync(
        Guid tenantId,
        int trendDays = 30,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(-trendDays);
        var midpoint = today.AddDays(-trendDays / 2);

        var trends = new MarketTrends
        {
            TrendDays = trendDays
        };

        // Calculate average price trend
        var currentPeriodListings = await _context.Listings
            .Where(l => l.IsActive && l.CreatedAt >= midpoint)
            .Select(l => l.Price)
            .ToListAsync(cancellationToken);

        var previousPeriodListings = await _context.Listings
            .Where(l => l.IsActive && l.CreatedAt >= startDate && l.CreatedAt < midpoint)
            .Select(l => l.Price)
            .ToListAsync(cancellationToken);

        trends.AveragePriceTrend = new TrendData
        {
            CurrentValue = currentPeriodListings.Count > 0 ? currentPeriodListings.Average() : 0,
            PreviousValue = previousPeriodListings.Count > 0 ? previousPeriodListings.Average() : 0
        };

        // Calculate inventory trend
        var currentInventory = await _context.Listings
            .CountAsync(l => l.IsActive && l.CreatedAt >= midpoint, cancellationToken);

        var previousInventory = await _context.Listings
            .CountAsync(l => l.IsActive && l.CreatedAt >= startDate && l.CreatedAt < midpoint, cancellationToken);

        trends.InventoryTrend = new TrendData
        {
            CurrentValue = currentInventory,
            PreviousValue = previousInventory
        };

        // Calculate new listings rate trend
        var halfPeriodDays = trendDays / 2;
        var currentNewListingsRate = currentInventory > 0 ? (decimal)currentInventory / halfPeriodDays : 0;
        var previousNewListingsRate = previousInventory > 0 ? (decimal)previousInventory / halfPeriodDays : 0;

        trends.NewListingsRateTrend = new TrendData
        {
            CurrentValue = currentNewListingsRate,
            PreviousValue = previousNewListingsRate
        };

        // Calculate days on market trend
        var currentDaysOnMarket = await _context.Listings
            .Where(l => l.IsActive && l.CreatedAt >= midpoint && l.DaysOnMarket.HasValue)
            .Select(l => l.DaysOnMarket!.Value)
            .ToListAsync(cancellationToken);

        var previousDaysOnMarket = await _context.Listings
            .Where(l => l.IsActive && l.CreatedAt >= startDate && l.CreatedAt < midpoint && l.DaysOnMarket.HasValue)
            .Select(l => l.DaysOnMarket!.Value)
            .ToListAsync(cancellationToken);

        trends.DaysOnMarketTrend = new TrendData
        {
            CurrentValue = currentDaysOnMarket.Count > 0 ? (decimal)currentDaysOnMarket.Average() : 0,
            PreviousValue = previousDaysOnMarket.Count > 0 ? (decimal)previousDaysOnMarket.Average() : 0
        };

        // Daily breakdown of new listings
        trends.DailyNewListings = await _context.Listings
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new DailyMetric
            {
                Date = g.Key,
                Count = g.Count(),
                AverageValue = g.Average(l => l.Price)
            })
            .OrderBy(m => m.Date)
            .ToListAsync(cancellationToken);

        // Daily breakdown of price changes
        trends.DailyPriceChanges = await _context.PriceHistory
            .Where(ph => ph.ObservedAt >= startDate && ph.PriceChange.HasValue)
            .GroupBy(ph => ph.ObservedAt.Date)
            .Select(g => new DailyMetric
            {
                Date = g.Key,
                Count = g.Count(),
                AverageValue = g.Average(ph => Math.Abs(ph.PriceChange!.Value))
            })
            .OrderBy(m => m.Date)
            .ToListAsync(cancellationToken);

        return trends;
    }

    /// <inheritdoc />
    public async Task<SystemMetrics> GetSystemMetricsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);

        var metrics = new SystemMetrics
        {
            TotalSearchSessions = await _context.SearchSessions.CountAsync(cancellationToken),
            SearchesLast24Hours = await _context.SearchSessions
                .CountAsync(s => s.CreatedAt >= yesterday, cancellationToken),
            SavedProfiles = await _context.SearchProfiles.CountAsync(cancellationToken),
            DatabaseStats = new DatabaseStats
            {
                TotalListings = await _context.Listings.CountAsync(cancellationToken),
                TotalPriceHistoryRecords = await _context.PriceHistory.CountAsync(cancellationToken),
                TotalVehicles = await _context.Vehicles.CountAsync(cancellationToken),
                TotalSearchSessions = await _context.SearchSessions.CountAsync(cancellationToken)
            }
        };

        // Note: ScraperHealth comes from the infrastructure layer's in-memory service
        // which will be injected separately in the command layer
        metrics.ScraperHealth = new Dictionary<string, ScraperStatus>();

        return metrics;
    }
}
