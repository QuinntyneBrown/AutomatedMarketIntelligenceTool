using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Analytics;

/// <summary>
/// Implementation of relisting pattern analysis service.
/// </summary>
public class RelistingPatternService : IRelistingPatternService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<RelistingPatternService> _logger;

    public RelistingPatternService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<RelistingPatternService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RelistingPatternAnalysis> AnalyzePatternsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing relisting patterns for tenant {TenantId}", tenantId);

        var relistedListings = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.RelistedCount > 0)
            .Include(l => l.Dealer)
            .ToListAsync(cancellationToken);

        if (!relistedListings.Any())
        {
            return new RelistingPatternAnalysis();
        }

        // Calculate statistics
        var totalRelistedVehicles = relistedListings.Count;
        var avgRelistCount = relistedListings.Average(l => l.RelistedCount);

        // Get time off market data
        var timesOffMarket = relistedListings
            .Where(l => l.RelistedAt.HasValue && l.DeactivatedAt.HasValue)
            .Select(l => (l.RelistedAt!.Value - l.DeactivatedAt!.Value).TotalDays)
            .OrderBy(d => d)
            .ToList();

        var medianTimeOffMarket = timesOffMarket.Any()
            ? GetMedian(timesOffMarket)
            : 0;

        // Get price changes
        var priceChanges = relistedListings
            .Where(l => l.PreviousPrice.HasValue)
            .Select(l => l.Price - l.PreviousPrice!.Value)
            .OrderBy(p => p)
            .ToList();

        var medianPriceChange = priceChanges.Any()
            ? GetMedian(priceChanges)
            : 0;

        // Top relisting makes
        var topMakes = relistedListings
            .GroupBy(l => l.Make ?? "Unknown")
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        // Frequent relisters
        var frequentRelisters = await GetFrequentRelistersAsync(tenantId, 3, cancellationToken);

        return new RelistingPatternAnalysis
        {
            TotalRelistedVehicles = totalRelistedVehicles,
            FrequentRelistersCount = frequentRelisters.Count,
            AverageRelistCount = avgRelistCount,
            MedianTimeOffMarket = medianTimeOffMarket,
            MedianPriceChange = medianPriceChange,
            TopRelistingMakes = topMakes,
            TopFrequentRelisters = frequentRelisters.Take(10).ToList()
        };
    }

    public async Task<DealerRelistingPattern> GetDealerPatternsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var dealer = await _context.Dealers
            .FirstOrDefaultAsync(d => d.DealerId == dealerId && d.TenantId == tenantId, cancellationToken);

        if (dealer == null)
        {
            throw new InvalidOperationException($"Dealer {dealerId.Value} not found.");
        }

        var relistedListings = await _context.Listings
            .Where(l => l.DealerId == dealerId && l.TenantId == tenantId && l.RelistedCount > 0)
            .OrderByDescending(l => l.RelistedAt)
            .ToListAsync(cancellationToken);

        if (!relistedListings.Any())
        {
            return new DealerRelistingPattern
            {
                DealerId = dealerId,
                DealerName = dealer.Name,
                TotalRelistedListings = 0,
                AverageRelistsPerVehicle = 0,
                AverageTimeOffMarketDays = 0,
                AveragePriceChangePercent = 0,
                IsFrequentRelister = false,
                RecentExamples = new List<RelistingExample>()
            };
        }

        // Calculate metrics
        var avgRelistsPerVehicle = relistedListings.Average(l => l.RelistedCount);

        var listingsWithTimeData = relistedListings
            .Where(l => l.RelistedAt.HasValue && l.DeactivatedAt.HasValue)
            .ToList();

        var avgTimeOffMarket = listingsWithTimeData.Any()
            ? listingsWithTimeData.Average(l => (l.RelistedAt!.Value - l.DeactivatedAt!.Value).TotalDays)
            : 0;

        var listingsWithPriceData = relistedListings
            .Where(l => l.PreviousPrice.HasValue && l.PreviousPrice > 0)
            .ToList();

        var avgPriceChangePercent = listingsWithPriceData.Any()
            ? listingsWithPriceData.Average(l => ((l.Price - l.PreviousPrice!.Value) / l.PreviousPrice.Value) * 100)
            : 0;

        var isFrequentRelister = relistedListings.Any(l => l.RelistedCount >= 3);

        // Get recent examples
        var recentExamples = relistedListings
            .Take(5)
            .Select(l => new RelistingExample
            {
                ListingId = l.ListingId,
                Make = l.Make ?? "Unknown",
                Model = l.Model ?? "Unknown",
                Year = l.Year ?? 0,
                RelistCount = l.RelistedCount,
                CurrentPrice = l.Price,
                OriginalPrice = l.PreviousPrice ?? l.Price,
                PriceChange = l.Price - (l.PreviousPrice ?? l.Price),
                FirstSeenAt = l.FirstSeenAt,
                LastRelistedAt = l.RelistedAt
            })
            .ToList();

        return new DealerRelistingPattern
        {
            DealerId = dealerId,
            DealerName = dealer.Name,
            TotalRelistedListings = relistedListings.Count,
            AverageRelistsPerVehicle = avgRelistsPerVehicle,
            AverageTimeOffMarketDays = avgTimeOffMarket,
            AveragePriceChangePercent = (decimal)avgPriceChangePercent,
            IsFrequentRelister = isFrequentRelister,
            RecentExamples = recentExamples
        };
    }

    public async Task<List<FrequentRelister>> GetFrequentRelistersAsync(
        Guid tenantId,
        int threshold = 3,
        CancellationToken cancellationToken = default)
    {
        // Get all dealers with their relisted listings
        var dealersWithRelistings = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.RelistedCount >= threshold)
            .GroupBy(l => l.DealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                RelistedCount = g.Count(),
                AvgRelistCount = g.Average(l => l.RelistedCount),
                AvgPriceReduction = g.Where(l => l.PreviousPrice.HasValue)
                    .Average(l => l.PreviousPrice!.Value - l.Price)
            })
            .ToListAsync(cancellationToken);

        var frequentRelisters = new List<FrequentRelister>();

        foreach (var item in dealersWithRelistings.OrderByDescending(d => d.RelistedCount))
        {
            if (item.DealerId == null) continue;

            var dealer = await _context.Dealers
                .FirstOrDefaultAsync(d => d.DealerId == item.DealerId && d.TenantId == tenantId, cancellationToken);

            if (dealer != null)
            {
                var vehiclesRelistedMultipleTimes = await _context.Listings
                    .CountAsync(l => l.DealerId == item.DealerId && l.TenantId == tenantId && l.RelistedCount >= 2, cancellationToken);

                frequentRelisters.Add(new FrequentRelister
                {
                    DealerId = item.DealerId,
                    DealerName = dealer.Name,
                    TotalRelistedCount = item.RelistedCount,
                    VehiclesRelistedMultipleTimes = vehiclesRelistedMultipleTimes,
                    AverageRelistCount = item.AvgRelistCount,
                    AveragePriceReduction = item.AvgPriceReduction,
                    City = dealer.City,
                    State = dealer.State
                });
            }
        }

        return frequentRelisters;
    }

    public async Task<List<RelistingTrend>> GetRelistingTrendsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveFromDate = fromDate ?? DateTime.UtcNow.AddMonths(-6);
        var effectiveToDate = toDate ?? DateTime.UtcNow;

        _logger.LogInformation(
            "Getting relisting trends for tenant {TenantId} from {FromDate} to {ToDate}",
            tenantId, effectiveFromDate, effectiveToDate);

        var relistedListings = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.RelistedAt.HasValue)
            .Where(l => l.RelistedAt >= effectiveFromDate && l.RelistedAt <= effectiveToDate)
            .ToListAsync(cancellationToken);

        // Group by week
        var trends = relistedListings
            .GroupBy(l => GetWeekStart(l.RelistedAt!.Value))
            .Select(g => new RelistingTrend
            {
                Date = g.Key,
                RelistCount = g.Count(),
                UniqueVehicles = g.Select(l => l.LinkedVehicleId).Distinct().Count(),
                AveragePriceChange = g.Where(l => l.PreviousPrice.HasValue)
                    .Average(l => l.Price - l.PreviousPrice!.Value),
                AverageDaysOffMarket = g.Where(l => l.DeactivatedAt.HasValue)
                    .Average(l => (l.RelistedAt!.Value - l.DeactivatedAt!.Value).TotalDays)
            })
            .OrderBy(t => t.Date)
            .ToList();

        return trends;
    }

    private static double GetMedian(List<double> values)
    {
        if (!values.Any()) return 0;

        var count = values.Count;
        if (count % 2 == 0)
        {
            return (values[count / 2 - 1] + values[count / 2]) / 2.0;
        }
        return values[count / 2];
    }

    private static decimal GetMedian(List<decimal> values)
    {
        if (!values.Any()) return 0;

        var count = values.Count;
        if (count % 2 == 0)
        {
            return (values[count / 2 - 1] + values[count / 2]) / 2.0m;
        }
        return values[count / 2];
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = date.DayOfWeek - DayOfWeek.Sunday;
        if (diff < 0) diff += 7;
        return date.AddDays(-diff).Date;
    }
}
