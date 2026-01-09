using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Implementation of dealer analytics service.
/// </summary>
public class DealerAnalyticsService : IDealerAnalyticsService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DealerAnalyticsService> _logger;

    // Reliability scoring constants
    private const int DaysOnMarketThreshold = 60;
    private const decimal DaysPenaltyDivisor = 10m;
    private const decimal MaxDaysPenalty = 30m;
    private const decimal MinActiveListingRatio = 0.5m;
    private const decimal ActiveRatioPenaltyMultiplier = 40m;
    private const decimal MaxRelistingPenalty = 30m;
    private const decimal MaxActiveRatioPenalty = 20m;
    private const decimal ConsistentInventoryBonus = 10m;
    private const int MinListingsForBonus = 10;
    private const int MinActiveListingsForBonus = 5;

    public DealerAnalyticsService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DealerAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DealerAnalytics> AnalyzeDealerAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing dealer {DealerId} for tenant {TenantId}", dealerId.Value, tenantId);

        var dealer = await _context.Dealers
            .FirstOrDefaultAsync(d => d.DealerId == dealerId && d.TenantId == tenantId, cancellationToken);

        if (dealer == null)
        {
            throw new InvalidOperationException($"Dealer {dealerId.Value} not found.");
        }

        // Get all listings for this dealer (active and historical)
        var listings = await _context.Listings
            .Where(l => l.DealerId == dealerId && l.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (listings.Count == 0)
        {
            _logger.LogWarning("No listings found for dealer {DealerId}", dealerId.Value);
            return CreateEmptyAnalytics(dealer);
        }

        // Calculate metrics
        var activeListings = listings.Count(l => l.IsActive);
        var relistedCount = listings.Count(l => l.RelistedCount > 0);
        var frequentRelisterFlag = listings.Count(l => l.RelistedCount >= 3) > 0;

        // Calculate average days on market
        var listingsWithDeactivation = listings
            .Where(l => l.DeactivatedAt.HasValue)
            .ToList();

        var avgDaysOnMarket = 0;
        if (listingsWithDeactivation.Any())
        {
            var totalDays = listingsWithDeactivation
                .Sum(l => (l.DeactivatedAt!.Value - l.FirstSeenDate).TotalDays);
            avgDaysOnMarket = (int)(totalDays / listingsWithDeactivation.Count);
        }

        // Calculate reliability score (0-100)
        var reliabilityScore = CalculateReliabilityScore(
            listings.Count,
            relistedCount,
            avgDaysOnMarket,
            activeListings);

        // Update dealer with analytics
        dealer.UpdateAnalytics(
            reliabilityScore,
            avgDaysOnMarket,
            listings.Count,
            frequentRelisterFlag);

        await _context.SaveChangesAsync(cancellationToken);

        return new DealerAnalytics
        {
            DealerId = dealerId,
            DealerName = dealer.Name,
            ReliabilityScore = reliabilityScore,
            AvgDaysOnMarket = avgDaysOnMarket,
            TotalListingsHistorical = listings.Count,
            ActiveListings = activeListings,
            RelistedCount = relistedCount,
            FrequentRelisterFlag = frequentRelisterFlag,
            LastAnalyzedAt = DateTime.UtcNow,
            City = dealer.City,
            State = dealer.State
        };
    }

    public async Task<List<DealerAnalytics>> AnalyzeAllDealersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing all dealers for tenant {TenantId}", tenantId);

        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var results = new List<DealerAnalytics>();

        foreach (var dealer in dealers)
        {
            try
            {
                var analytics = await AnalyzeDealerAsync(tenantId, dealer.DealerId, cancellationToken);
                results.Add(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing dealer {DealerId}", dealer.DealerId.Value);
            }
        }

        return results;
    }

    public async Task<DealerAnalytics?> GetDealerAnalyticsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var dealer = await _context.Dealers
            .FirstOrDefaultAsync(d => d.DealerId == dealerId && d.TenantId == tenantId, cancellationToken);

        if (dealer == null || !dealer.LastAnalyzedAt.HasValue)
        {
            return null;
        }

        var activeListings = await _context.Listings
            .CountAsync(l => l.DealerId == dealerId && l.TenantId == tenantId && l.IsActive, cancellationToken);

        var relistedCount = await _context.Listings
            .CountAsync(l => l.DealerId == dealerId && l.TenantId == tenantId && l.RelistedCount > 0, cancellationToken);

        return new DealerAnalytics
        {
            DealerId = dealerId,
            DealerName = dealer.Name,
            ReliabilityScore = dealer.ReliabilityScore ?? 0,
            AvgDaysOnMarket = dealer.AvgDaysOnMarket ?? 0,
            TotalListingsHistorical = dealer.TotalListingsHistorical,
            ActiveListings = activeListings,
            RelistedCount = relistedCount,
            FrequentRelisterFlag = dealer.FrequentRelisterFlag,
            LastAnalyzedAt = dealer.LastAnalyzedAt,
            City = dealer.City,
            State = dealer.State
        };
    }

    public async Task<List<DealerInventorySnapshot>> GetInventoryHistoryAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveFromDate = fromDate ?? DateTime.UtcNow.AddMonths(-6);
        var effectiveToDate = toDate ?? DateTime.UtcNow;

        _logger.LogInformation(
            "Getting inventory history for dealer {DealerId} from {FromDate} to {ToDate}",
            dealerId.Value, effectiveFromDate, effectiveToDate);

        // Group listings by date and generate snapshots
        var listings = await _context.Listings
            .Where(l => l.DealerId == dealerId && l.TenantId == tenantId)
            .Where(l => l.FirstSeenDate >= effectiveFromDate && l.FirstSeenDate <= effectiveToDate)
            .OrderBy(l => l.FirstSeenDate)
            .ToListAsync(cancellationToken);

        // Create weekly snapshots
        var snapshots = new List<DealerInventorySnapshot>();
        var currentDate = effectiveFromDate.Date;

        while (currentDate <= effectiveToDate)
        {
            var weekEnd = currentDate.AddDays(7);
            var weekListings = listings
                .Where(l => l.FirstSeenDate >= currentDate && l.FirstSeenDate < weekEnd)
                .ToList();

            var activeInWeek = listings
                .Where(l => l.FirstSeenDate < weekEnd && (!l.DeactivatedAt.HasValue || l.DeactivatedAt >= currentDate))
                .ToList();

            var deactivatedInWeek = listings
                .Where(l => l.DeactivatedAt >= currentDate && l.DeactivatedAt < weekEnd)
                .Count();

            var makeBreakdown = activeInWeek
                .GroupBy(l => l.Make ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            if (activeInWeek.Any())
            {
                snapshots.Add(new DealerInventorySnapshot
                {
                    SnapshotDate = currentDate,
                    TotalListings = activeInWeek.Count,
                    NewListings = weekListings.Count,
                    DeactivatedListings = deactivatedInWeek,
                    AveragePrice = activeInWeek.Average(l => l.Price),
                    AverageMileage = (int)activeInWeek.Where(l => l.Mileage.HasValue).Average(l => l.Mileage ?? 0),
                    MakeBreakdown = makeBreakdown
                });
            }

            currentDate = weekEnd;
        }

        return snapshots;
    }

    public async Task<List<DealerAnalytics>> GetLowReliabilityDealersAsync(
        Guid tenantId,
        decimal threshold = 50m,
        CancellationToken cancellationToken = default)
    {
        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId && d.ReliabilityScore.HasValue && d.ReliabilityScore < threshold)
            .ToListAsync(cancellationToken);

        var results = new List<DealerAnalytics>();

        foreach (var dealer in dealers)
        {
            var analytics = await GetDealerAnalyticsAsync(tenantId, dealer.DealerId, cancellationToken);
            if (analytics != null)
            {
                results.Add(analytics);
            }
        }

        return results.OrderBy(a => a.ReliabilityScore).ToList();
    }

    private decimal CalculateReliabilityScore(
        int totalListings,
        int relistedCount,
        int avgDaysOnMarket,
        int activeListings)
    {
        // Base score starts at 100
        decimal score = 100m;

        // Penalty for relisting rate (max penalty defined by MaxRelistingPenalty)
        if (totalListings > 0)
        {
            var relistingRate = (decimal)relistedCount / totalListings;
            score -= Math.Min(relistingRate * 100m * (MaxRelistingPenalty / 100m), MaxRelistingPenalty);
        }

        // Penalty for long days on market (max penalty defined by MaxDaysPenalty)
        // Over DaysOnMarketThreshold is considered poor
        if (avgDaysOnMarket > DaysOnMarketThreshold)
        {
            var daysPenalty = Math.Min((avgDaysOnMarket - DaysOnMarketThreshold) / DaysPenaltyDivisor, MaxDaysPenalty);
            score -= daysPenalty;
        }

        // Penalty for low active listing ratio (max penalty defined by MaxActiveRatioPenalty)
        if (totalListings > 0)
        {
            var activeRatio = (decimal)activeListings / totalListings;
            if (activeRatio < MinActiveListingRatio)
            {
                score -= (MinActiveListingRatio - activeRatio) * ActiveRatioPenaltyMultiplier;
            }
        }

        // Bonus for consistent inventory
        if (totalListings >= MinListingsForBonus && activeListings >= MinActiveListingsForBonus)
        {
            score += ConsistentInventoryBonus;
        }

        // Ensure score is between 0 and 100
        return Math.Max(0m, Math.Min(100m, score));
    }

    private DealerAnalytics CreateEmptyAnalytics(Dealer dealer)
    {
        return new DealerAnalytics
        {
            DealerId = dealer.DealerId,
            DealerName = dealer.Name,
            ReliabilityScore = 0m,
            AvgDaysOnMarket = 0,
            TotalListingsHistorical = 0,
            ActiveListings = 0,
            RelistedCount = 0,
            FrequentRelisterFlag = false,
            LastAnalyzedAt = null,
            City = dealer.City,
            State = dealer.State
        };
    }
}
