using System.Diagnostics;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;

/// <summary>
/// Service for tracking and analyzing dealer inventory history.
/// </summary>
public class InventoryHistoryService : IInventoryHistoryService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<InventoryHistoryService> _logger;

    public InventoryHistoryService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<InventoryHistoryService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InventorySnapshot> CreateSnapshotAsync(
        Guid tenantId,
        DealerId dealerId,
        SnapshotPeriodType periodType = SnapshotPeriodType.Daily,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating {PeriodType} snapshot for dealer {DealerId}", periodType, dealerId.Value);

        var snapshotDate = DateTime.UtcNow.Date;

        // Get current listings for the dealer
        var listings = await _context.Listings
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.DealerId == dealerId)
            .ToListAsync(cancellationToken);

        var activeListings = listings.Where(l => l.IsActive).ToList();

        // Get previous snapshot for comparison
        var previousSnapshot = await GetLatestSnapshotAsync(tenantId, dealerId, cancellationToken);

        // Calculate changes since last snapshot
        var previousSnapshotDate = previousSnapshot?.SnapshotDate ?? snapshotDate.AddDays(-1);
        var newListings = listings.Count(l => l.CreatedAt >= previousSnapshotDate);
        var removedListings = previousSnapshot != null
            ? Math.Max(0, previousSnapshot.TotalListings - activeListings.Count + newListings)
            : 0;

        // Get price changes
        var priceChanges = await _context.PriceHistory
            .Where(ph => ph.TenantId == tenantId)
            .Where(ph => ph.ObservedAt >= previousSnapshotDate)
            .Join(
                _context.Listings.Where(l => l.DealerId == dealerId),
                ph => ph.ListingId,
                l => l.ListingId,
                (ph, l) => ph)
            .ToListAsync(cancellationToken);

        // Create the snapshot
        var snapshot = InventorySnapshot.Create(tenantId, dealerId, snapshotDate, periodType);

        // Set inventory counts
        var soldCount = 0; // Would need sold tracking
        var expiredCount = listings.Count(l => !l.IsActive && l.DeactivatedAt >= previousSnapshotDate);
        var relistedCount = listings.Count(l => l.RelistedCount > 0 && l.CreatedAt >= previousSnapshotDate);

        snapshot.SetInventoryCounts(
            activeListings.Count,
            newListings,
            removedListings,
            soldCount,
            expiredCount,
            relistedCount);

        // Set inventory values
        if (activeListings.Count > 0)
        {
            var prices = activeListings.Select(l => l.Price).OrderBy(p => p).ToList();
            snapshot.SetInventoryValues(
                prices.Sum(),
                prices.Average(),
                prices[prices.Count / 2],
                prices.Min(),
                prices.Max());
        }

        // Set price changes
        var increases = priceChanges.Count(pc => pc.PriceChange > 0);
        var decreases = priceChanges.Count(pc => pc.PriceChange < 0);
        var totalReduction = priceChanges.Where(pc => pc.PriceChange < 0).Sum(pc => Math.Abs(pc.PriceChange ?? 0));
        var avgChange = priceChanges.Count > 0 ? priceChanges.Average(pc => pc.PriceChange ?? 0) : 0;

        snapshot.SetPriceChanges(increases, decreases, totalReduction, avgChange);

        // Set age distribution
        var now = DateTime.UtcNow;
        var under30 = activeListings.Count(l => (now - l.FirstSeenDate).TotalDays <= 30);
        var days30To60 = activeListings.Count(l => (now - l.FirstSeenDate).TotalDays > 30 && (now - l.FirstSeenDate).TotalDays <= 60);
        var days60To90 = activeListings.Count(l => (now - l.FirstSeenDate).TotalDays > 60 && (now - l.FirstSeenDate).TotalDays <= 90);
        var over90 = activeListings.Count(l => (now - l.FirstSeenDate).TotalDays > 90);
        var avgDays = activeListings.Count > 0 ? activeListings.Average(l => (now - l.FirstSeenDate).TotalDays) : 0;

        snapshot.SetAgeDistribution(under30, days30To60, days60To90, over90, avgDays);

        // Set distributions
        var byMake = activeListings
            .GroupBy(l => l.Make)
            .ToDictionary(g => g.Key, g => g.Count());
        var byYear = activeListings
            .GroupBy(l => l.Year)
            .ToDictionary(g => g.Key, g => g.Count());
        var byCondition = activeListings
            .GroupBy(l => l.Condition.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        snapshot.SetDistributions(byMake, byYear, byCondition);

        // Set trends
        snapshot.SetTrends(previousSnapshot);

        // Save
        _context.InventorySnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {PeriodType} snapshot for dealer {DealerId}: {Listings} listings, ${Value:N0} total value",
            periodType, dealerId.Value, snapshot.TotalListings, snapshot.TotalInventoryValue);

        return snapshot;
    }

    public async Task<SnapshotBatchResult> CreateSnapshotsForAllDealersAsync(
        Guid tenantId,
        SnapshotPeriodType periodType = SnapshotPeriodType.Daily,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SnapshotBatchResult();

        var dealerIds = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.DealerId)
            .ToListAsync(cancellationToken);

        result.TotalDealers = dealerIds.Count;

        _logger.LogInformation("Creating {PeriodType} snapshots for {Count} dealers", periodType, dealerIds.Count);

        for (var i = 0; i < dealerIds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await CreateSnapshotAsync(tenantId, dealerIds[i], periodType, cancellationToken);
                result.SnapshotsCreated++;
            }
            catch (Exception ex)
            {
                result.Failures++;
                result.Errors.Add($"Failed for dealer {dealerIds[i].Value}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to create snapshot for dealer {DealerId}", dealerIds[i].Value);
            }

            progress?.Report(new BatchProgress(i + 1, dealerIds.Count,
                $"Created {result.SnapshotsCreated}/{dealerIds.Count} snapshots"));
        }

        stopwatch.Stop();
        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Completed snapshot batch. Created: {Created}, Failures: {Failures}, Time: {Time}ms",
            result.SnapshotsCreated, result.Failures, result.ProcessingTimeMs);

        return result;
    }

    public async Task<InventorySnapshot?> GetLatestSnapshotAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.DealerId == dealerId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<InventorySnapshot>> GetDealerSnapshotsAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        SnapshotPeriodType? periodType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.DealerId == dealerId);

        if (fromDate.HasValue)
            query = query.Where(s => s.SnapshotDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.SnapshotDate <= toDate.Value);

        if (periodType.HasValue)
            query = query.Where(s => s.PeriodType == periodType.Value);

        return await query
            .OrderByDescending(s => s.SnapshotDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InventorySnapshotSummary>> GetDealerSnapshotSummariesAsync(
        Guid tenantId,
        DealerId dealerId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.DealerId == dealerId)
            .OrderByDescending(s => s.SnapshotDate);

        if (limit.HasValue)
            query = (IOrderedQueryable<InventorySnapshot>)query.Take(limit.Value);

        var snapshots = await query.ToListAsync(cancellationToken);
        return snapshots.Select(s => s.GetSummary()).ToList();
    }

    public async Task<InventoryTrendData> GetInventoryTrendsAsync(
        Guid tenantId,
        DealerId dealerId,
        int periodDays = 30,
        CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.Date.AddDays(-periodDays);

        var snapshots = await _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.DealerId == dealerId)
            .Where(s => s.SnapshotDate >= fromDate)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(cancellationToken);

        var trendData = new InventoryTrendData
        {
            DealerId = dealerId,
            PeriodStart = fromDate,
            PeriodEnd = DateTime.UtcNow.Date,
            DataPoints = snapshots.Select(s => new InventoryDataPoint
            {
                Date = s.SnapshotDate,
                TotalListings = s.TotalListings,
                TotalValue = s.TotalInventoryValue,
                AveragePrice = s.AverageListingPrice,
                NewListings = s.NewListingsAdded,
                RemovedListings = s.ListingsRemoved
            }).ToList()
        };

        if (snapshots.Count >= 2)
        {
            var first = snapshots.First();
            var last = snapshots.Last();

            trendData.InventoryChangePercent = first.TotalListings > 0
                ? (double)(last.TotalListings - first.TotalListings) / first.TotalListings * 100
                : 0;

            trendData.ValueChangePercent = first.TotalInventoryValue > 0
                ? (double)(last.TotalInventoryValue - first.TotalInventoryValue) / (double)first.TotalInventoryValue * 100
                : 0;

            trendData.InventoryTrend = trendData.InventoryChangePercent switch
            {
                > 10 => TrendDirection.Increasing,
                < -10 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };

            trendData.ValueTrend = trendData.ValueChangePercent switch
            {
                > 10 => TrendDirection.Increasing,
                < -10 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };

            // Calculate average turnover
            var totalRemoved = snapshots.Sum(s => s.ListingsSold + s.ListingsExpired);
            var avgListings = snapshots.Average(s => s.TotalListings);
            trendData.AverageTurnoverRate = avgListings > 0 ? (double)totalRemoved / avgListings : 0;
        }

        return trendData;
    }

    public async Task<InventoryComparison> CompareInventoryAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime date1,
        DateTime date2,
        CancellationToken cancellationToken = default)
    {
        var snapshot1 = await _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.DealerId == dealerId)
            .Where(s => s.SnapshotDate.Date == date1.Date)
            .FirstOrDefaultAsync(cancellationToken);

        var snapshot2 = await _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.DealerId == dealerId)
            .Where(s => s.SnapshotDate.Date == date2.Date)
            .FirstOrDefaultAsync(cancellationToken);

        var comparison = new InventoryComparison
        {
            DealerId = dealerId,
            Date1 = date1,
            Date2 = date2,
            ListingsDate1 = snapshot1?.TotalListings ?? 0,
            ListingsDate2 = snapshot2?.TotalListings ?? 0,
            ValueDate1 = snapshot1?.TotalInventoryValue ?? 0,
            ValueDate2 = snapshot2?.TotalInventoryValue ?? 0
        };

        comparison.ListingChange = comparison.ListingsDate2 - comparison.ListingsDate1;
        comparison.ListingChangePercent = comparison.ListingsDate1 > 0
            ? (double)comparison.ListingChange / comparison.ListingsDate1 * 100
            : 0;

        comparison.ValueChange = comparison.ValueDate2 - comparison.ValueDate1;
        comparison.ValueChangePercent = comparison.ValueDate1 > 0
            ? (double)(comparison.ValueChange / comparison.ValueDate1) * 100
            : 0;

        // Calculate added/removed (simplified)
        if (snapshot2 != null)
        {
            comparison.ListingsAdded = snapshot2.NewListingsAdded;
            comparison.ListingsRemoved = snapshot2.ListingsRemoved;
        }

        return comparison;
    }

    public async Task<int> CleanupOldSnapshotsAsync(
        Guid tenantId,
        int retentionDays = 365,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-retentionDays);

        // Keep weekly/monthly snapshots, only delete daily ones older than retention
        var oldSnapshots = await _context.InventorySnapshots
            .Where(s => s.TenantId == tenantId)
            .Where(s => s.SnapshotDate < cutoffDate)
            .Where(s => s.PeriodType == SnapshotPeriodType.Daily)
            .ToListAsync(cancellationToken);

        _context.InventorySnapshots.RemoveRange(oldSnapshots);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} old daily snapshots", oldSnapshots.Count);

        return oldSnapshots.Count;
    }

    public async Task<MarketInventoryStatistics> GetMarketInventoryStatisticsAsync(
        Guid tenantId,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var date = asOfDate?.Date ?? DateTime.UtcNow.Date;
        var weekAgo = date.AddDays(-7);

        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .CountAsync(cancellationToken);

        var listings = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.IsActive)
            .ToListAsync(cancellationToken);

        var newListings7Days = await _context.Listings
            .CountAsync(l => l.TenantId == tenantId && l.CreatedAt >= weekAgo, cancellationToken);

        var removedListings7Days = await _context.Listings
            .CountAsync(l => l.TenantId == tenantId && !l.IsActive && l.DeactivatedAt >= weekAgo, cancellationToken);

        return new MarketInventoryStatistics
        {
            AsOfDate = date,
            TotalDealers = dealers,
            TotalListings = listings.Count,
            TotalInventoryValue = listings.Sum(l => l.Price),
            AverageListingPrice = listings.Count > 0 ? listings.Average(l => l.Price) : 0,
            AverageDaysOnMarket = listings.Count > 0 && listings.Any(l => l.DaysOnMarket.HasValue)
                ? listings.Where(l => l.DaysOnMarket.HasValue).Average(l => l.DaysOnMarket!.Value)
                : 0,
            NewListingsLast7Days = newListings7Days,
            RemovedListingsLast7Days = removedListings7Days,
            ListingsByMake = listings
                .GroupBy(l => l.Make)
                .ToDictionary(g => g.Key, g => g.Count()),
            ListingsByYear = listings
                .GroupBy(l => l.Year)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<List<DealerInventoryChange>> GetDealersWithSignificantChangesAsync(
        Guid tenantId,
        double changeThresholdPercent = 20.0,
        int periodDays = 7,
        CancellationToken cancellationToken = default)
    {
        var changes = new List<DealerInventoryChange>();
        var fromDate = DateTime.UtcNow.Date.AddDays(-periodDays);

        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var dealer in dealers)
        {
            var oldSnapshot = await _context.InventorySnapshots
                .Where(s => s.TenantId == tenantId && s.DealerId == dealer.DealerId)
                .Where(s => s.SnapshotDate >= fromDate)
                .OrderBy(s => s.SnapshotDate)
                .FirstOrDefaultAsync(cancellationToken);

            var newSnapshot = await GetLatestSnapshotAsync(tenantId, dealer.DealerId, cancellationToken);

            if (oldSnapshot != null && newSnapshot != null && oldSnapshot.TotalListings > 0)
            {
                var changeAmount = newSnapshot.TotalListings - oldSnapshot.TotalListings;
                var changePercent = (double)changeAmount / oldSnapshot.TotalListings * 100;

                if (Math.Abs(changePercent) >= changeThresholdPercent)
                {
                    var changeType = changeAmount > 0
                        ? (changePercent > 50 ? ChangeType.MassAddition : ChangeType.SignificantIncrease)
                        : (changePercent < -50 ? ChangeType.MassRemoval : ChangeType.SignificantDecrease);

                    changes.Add(new DealerInventoryChange
                    {
                        DealerId = dealer.DealerId,
                        DealerName = dealer.Name,
                        PreviousListingCount = oldSnapshot.TotalListings,
                        CurrentListingCount = newSnapshot.TotalListings,
                        ChangeAmount = changeAmount,
                        ChangePercent = changePercent,
                        Type = changeType
                    });
                }
            }
        }

        return changes.OrderByDescending(c => Math.Abs(c.ChangePercent)).ToList();
    }

    public async Task<int> AggregateSnapshotsAsync(
        Guid tenantId,
        SnapshotPeriodType targetPeriod,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        // This would aggregate daily snapshots into weekly/monthly
        // For now, simplified implementation
        var startDate = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
        var aggregated = 0;

        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.DealerId)
            .ToListAsync(cancellationToken);

        foreach (var dealerId in dealers)
        {
            var snapshots = await _context.InventorySnapshots
                .Where(s => s.TenantId == tenantId && s.DealerId == dealerId)
                .Where(s => s.PeriodType == SnapshotPeriodType.Daily)
                .Where(s => s.SnapshotDate >= startDate)
                .ToListAsync(cancellationToken);

            if (snapshots.Count > 0 && targetPeriod == SnapshotPeriodType.Weekly)
            {
                // Group by week and create weekly snapshots
                var weeks = snapshots.GroupBy(s => GetWeekStart(s.SnapshotDate));

                foreach (var week in weeks)
                {
                    var existingWeekly = await _context.InventorySnapshots
                        .AnyAsync(s =>
                            s.TenantId == tenantId &&
                            s.DealerId == dealerId &&
                            s.PeriodType == SnapshotPeriodType.Weekly &&
                            s.SnapshotDate == week.Key,
                            cancellationToken);

                    if (!existingWeekly)
                    {
                        var weeklySnapshot = CreateAggregatedSnapshot(
                            tenantId, dealerId, week.Key, SnapshotPeriodType.Weekly, week.ToList());

                        _context.InventorySnapshots.Add(weeklySnapshot);
                        aggregated++;
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} aggregated {Period} snapshots", aggregated, targetPeriod);

        return aggregated;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = date.DayOfWeek - DayOfWeek.Sunday;
        return date.Date.AddDays(-diff);
    }

    private static InventorySnapshot CreateAggregatedSnapshot(
        Guid tenantId,
        DealerId dealerId,
        DateTime periodStart,
        SnapshotPeriodType periodType,
        List<InventorySnapshot> dailySnapshots)
    {
        var snapshot = InventorySnapshot.Create(tenantId, dealerId, periodStart, periodType);

        // Use the last day's values for counts
        var lastDay = dailySnapshots.OrderByDescending(s => s.SnapshotDate).First();

        // Sum up the activity metrics
        snapshot.SetInventoryCounts(
            lastDay.TotalListings,
            dailySnapshots.Sum(s => s.NewListingsAdded),
            dailySnapshots.Sum(s => s.ListingsRemoved),
            dailySnapshots.Sum(s => s.ListingsSold),
            dailySnapshots.Sum(s => s.ListingsExpired),
            dailySnapshots.Sum(s => s.ListingsRelisted));

        snapshot.SetInventoryValues(
            lastDay.TotalInventoryValue,
            dailySnapshots.Average(s => s.AverageListingPrice),
            lastDay.MedianListingPrice,
            dailySnapshots.Min(s => s.MinListingPrice),
            dailySnapshots.Max(s => s.MaxListingPrice));

        snapshot.SetPriceChanges(
            dailySnapshots.Sum(s => s.PriceIncreasesCount),
            dailySnapshots.Sum(s => s.PriceDecreasesCount),
            dailySnapshots.Sum(s => s.TotalPriceReduction),
            dailySnapshots.Average(s => s.AveragePriceChange));

        snapshot.SetAgeDistribution(
            lastDay.ListingsUnder30Days,
            lastDay.Listings30To60Days,
            lastDay.Listings60To90Days,
            lastDay.ListingsOver90Days,
            dailySnapshots.Average(s => s.AverageDaysOnMarket));

        snapshot.SetDistributions(
            lastDay.CountByMake,
            lastDay.CountByYear,
            lastDay.CountByCondition);

        return snapshot;
    }
}
