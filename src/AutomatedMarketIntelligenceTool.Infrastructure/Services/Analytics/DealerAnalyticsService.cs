using System.Diagnostics;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;

/// <summary>
/// Service for analyzing dealer performance and calculating reliability metrics.
/// </summary>
public class DealerAnalyticsService : IDealerAnalyticsService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DealerAnalyticsService> _logger;

    public DealerAnalyticsService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DealerAnalyticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DealerMetrics> GetOrCreateDealerMetricsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _context.DealerMetrics
            .FirstOrDefaultAsync(m =>
                m.TenantId == tenantId &&
                m.DealerId == dealerId,
                cancellationToken);

        if (metrics == null)
        {
            metrics = DealerMetrics.Create(tenantId, dealerId);
            _context.DealerMetrics.Add(metrics);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created new dealer metrics for dealer {DealerId}",
                dealerId.Value);
        }

        return metrics;
    }

    public async Task<DealerMetrics?> GetDealerMetricsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DealerMetrics
            .FirstOrDefaultAsync(m =>
                m.TenantId == tenantId &&
                m.DealerId == dealerId,
                cancellationToken);
    }

    public async Task<DealerMetrics> AnalyzeDealerAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting analysis for dealer {DealerId}",
            dealerId.Value);

        var stopwatch = Stopwatch.StartNew();

        // Get or create metrics
        var metrics = await GetOrCreateDealerMetricsAsync(tenantId, dealerId, cancellationToken);

        // Get dealer's listings
        var listings = await _context.Listings
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.DealerId == dealerId)
            .ToListAsync(cancellationToken);

        if (listings.Count == 0)
        {
            _logger.LogWarning(
                "No listings found for dealer {DealerId}",
                dealerId.Value);
            return metrics;
        }

        var activeListings = listings.Where(l => l.IsActive).ToList();
        var inactiveListings = listings.Where(l => !l.IsActive).ToList();

        // Calculate metrics
        var updateData = new DealerMetricsUpdateData
        {
            TotalListingsHistorical = listings.Count,
            ActiveListingsCount = activeListings.Count,
            SoldListingsCount = 0, // Would need sold flag
            ExpiredListingsCount = inactiveListings.Count,

            AverageDaysOnMarket = CalculateAverageDaysOnMarket(listings),
            MedianDaysOnMarket = CalculateMedianDaysOnMarket(listings),
            MinDaysOnMarket = CalculateMinDaysOnMarket(listings),
            MaxDaysOnMarket = CalculateMaxDaysOnMarket(listings),

            AverageListingPrice = CalculateAveragePrice(activeListings),
            AveragePriceReduction = await CalculateAveragePriceReductionAsync(tenantId, dealerId, cancellationToken),
            AveragePriceReductionPercent = await CalculateAveragePriceReductionPercentAsync(tenantId, dealerId, cancellationToken),
            PriceReductionCount = await GetPriceReductionCountAsync(tenantId, dealerId, cancellationToken),

            RelistingCount = listings.Sum(l => l.RelistedCount),
            RelistingRate = listings.Count > 0
                ? (double)listings.Count(l => l.RelistedCount > 0) / listings.Count
                : 0,

            VinProvidedRate = CalculateVinProvidedRate(listings),
            ImageProvidedRate = CalculateImageProvidedRate(listings),
            DescriptionQualityScore = CalculateDescriptionQualityScore(listings)
        };

        // Update metrics
        metrics.UpdateMetrics(updateData);
        await _context.SaveChangesAsync(cancellationToken);

        // Update denormalized dealer fields
        var dealer = await _context.Dealers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DealerId == dealerId, cancellationToken);

        if (dealer != null)
        {
            dealer.UpdateReliabilityMetrics(
                metrics.ReliabilityScore,
                (int)metrics.AverageDaysOnMarket,
                metrics.TotalListingsHistorical,
                metrics.IsFrequentRelister);
            await _context.SaveChangesAsync(cancellationToken);
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Completed analysis for dealer {DealerId} in {ElapsedMs}ms. Score: {Score:F1}",
            dealerId.Value, stopwatch.ElapsedMilliseconds, metrics.ReliabilityScore);

        return metrics;
    }

    public async Task<DealerAnalysisBatchResult> AnalyzeAllDealersAsync(
        Guid tenantId,
        bool forceReanalysis = false,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DealerAnalysisBatchResult();

        var dealerIds = forceReanalysis
            ? await _context.Dealers
                .Where(d => d.TenantId == tenantId)
                .Select(d => d.DealerId)
                .ToListAsync(cancellationToken)
            : await GetDealersNeedingAnalysisAsync(tenantId, TimeSpan.FromDays(7), null, cancellationToken);

        result.TotalDealersAnalyzed = dealerIds.Count;

        for (var i = 0; i < dealerIds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await AnalyzeDealerAsync(tenantId, dealerIds[i], cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Failed to analyze dealer {dealerIds[i].Value}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to analyze dealer {DealerId}", dealerIds[i].Value);
            }

            progress?.Report(new BatchProgress(i + 1, dealerIds.Count, $"Analyzed {i + 1}/{dealerIds.Count} dealers"));
        }

        stopwatch.Stop();
        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Completed batch analysis. Success: {Success}, Failures: {Failures}, Time: {Time}ms",
            result.SuccessCount, result.FailureCount, result.ProcessingTimeMs);

        return result;
    }

    public async Task<List<DealerWithMetrics>> GetDealersByReliabilityScoreAsync(
        Guid tenantId,
        decimal minScore,
        decimal maxScore,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = from d in _context.Dealers
                    join m in _context.DealerMetrics on d.DealerId equals m.DealerId into metricsJoin
                    from metrics in metricsJoin.DefaultIfEmpty()
                    where d.TenantId == tenantId &&
                          (metrics == null || (metrics.ReliabilityScore >= minScore && metrics.ReliabilityScore <= maxScore))
                    orderby metrics != null ? metrics.ReliabilityScore : 50 descending
                    select new DealerWithMetrics
                    {
                        Dealer = d,
                        Metrics = metrics
                    };

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<DealerWithMetrics>> GetFrequentRelistersAsync(
        Guid tenantId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = from d in _context.Dealers
                    join m in _context.DealerMetrics on d.DealerId equals m.DealerId
                    where d.TenantId == tenantId && m.IsFrequentRelister
                    orderby m.RelistingRate descending
                    select new DealerWithMetrics
                    {
                        Dealer = d,
                        Metrics = m
                    };

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<DealerWithMetrics>> GetTopDealersAsync(
        Guid tenantId,
        DealerRankingCriteria criteria,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = from d in _context.Dealers
                    join m in _context.DealerMetrics on d.DealerId equals m.DealerId into metricsJoin
                    from metrics in metricsJoin.DefaultIfEmpty()
                    where d.TenantId == tenantId
                    select new { Dealer = d, Metrics = metrics };

        var orderedQuery = criteria switch
        {
            DealerRankingCriteria.ReliabilityScore =>
                query.OrderByDescending(x => x.Metrics != null ? x.Metrics.ReliabilityScore : 0),
            DealerRankingCriteria.LowestDaysOnMarket =>
                query.OrderBy(x => x.Metrics != null ? x.Metrics.AverageDaysOnMarket : double.MaxValue),
            DealerRankingCriteria.HighestVolume =>
                query.OrderByDescending(x => x.Metrics != null ? x.Metrics.TotalListingsHistorical : 0),
            DealerRankingCriteria.BestDataQuality =>
                query.OrderByDescending(x => x.Metrics != null ? x.Metrics.VinProvidedRate : 0),
            DealerRankingCriteria.LowestRelistingRate =>
                query.OrderBy(x => x.Metrics != null ? x.Metrics.RelistingRate : 1),
            _ => query.OrderByDescending(x => x.Metrics != null ? x.Metrics.ReliabilityScore : 0)
        };

        return await orderedQuery
            .Take(limit)
            .Select(x => new DealerWithMetrics { Dealer = x.Dealer, Metrics = x.Metrics })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DealerId>> GetDealersNeedingAnalysisAsync(
        Guid tenantId,
        TimeSpan analysisInterval,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow - analysisInterval;

        var query = _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .Where(d => d.LastAnalyzedAt == null || d.LastAnalyzedAt < threshold)
            .OrderBy(d => d.LastAnalyzedAt ?? DateTime.MinValue)
            .Select(d => d.DealerId);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<DealerMarketStatistics> GetDealerMarketStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var metrics = await _context.DealerMetrics
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var activeListingsCount = await _context.Listings
            .Where(l => l.TenantId == tenantId && l.IsActive)
            .CountAsync(cancellationToken);

        var scores = metrics.Select(m => m.ReliabilityScore).OrderBy(s => s).ToList();

        return new DealerMarketStatistics
        {
            TotalDealers = dealers.Count,
            ActiveDealers = dealers.Count(d => d.ListingCount > 0),
            AverageReliabilityScore = scores.Count > 0 ? scores.Average() : 50,
            MedianReliabilityScore = scores.Count > 0 ? scores[scores.Count / 2] : 50,
            FrequentRelisterCount = metrics.Count(m => m.IsFrequentRelister),
            AverageDaysOnMarket = metrics.Count > 0 ? metrics.Average(m => m.AverageDaysOnMarket) : 0,
            TotalActiveListings = activeListingsCount,
            DealersByState = dealers
                .Where(d => !string.IsNullOrEmpty(d.State))
                .GroupBy(d => d.State!)
                .ToDictionary(g => g.Key, g => g.Count()),
            DealersByCity = dealers
                .Where(d => !string.IsNullOrEmpty(d.City))
                .GroupBy(d => d.City!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    // Helper methods
    private static double CalculateAverageDaysOnMarket(List<Core.Models.ListingAggregate.Listing> listings)
    {
        var daysValues = listings
            .Where(l => l.DaysOnMarket.HasValue)
            .Select(l => l.DaysOnMarket!.Value)
            .ToList();

        return daysValues.Count > 0 ? daysValues.Average() : 0;
    }

    private static double CalculateMedianDaysOnMarket(List<Core.Models.ListingAggregate.Listing> listings)
    {
        var daysValues = listings
            .Where(l => l.DaysOnMarket.HasValue)
            .Select(l => l.DaysOnMarket!.Value)
            .OrderBy(d => d)
            .ToList();

        if (daysValues.Count == 0) return 0;
        return daysValues[daysValues.Count / 2];
    }

    private static int CalculateMinDaysOnMarket(List<Core.Models.ListingAggregate.Listing> listings)
    {
        var daysValues = listings
            .Where(l => l.DaysOnMarket.HasValue)
            .Select(l => l.DaysOnMarket!.Value)
            .ToList();

        return daysValues.Count > 0 ? daysValues.Min() : 0;
    }

    private static int CalculateMaxDaysOnMarket(List<Core.Models.ListingAggregate.Listing> listings)
    {
        var daysValues = listings
            .Where(l => l.DaysOnMarket.HasValue)
            .Select(l => l.DaysOnMarket!.Value)
            .ToList();

        return daysValues.Count > 0 ? daysValues.Max() : 0;
    }

    private static decimal CalculateAveragePrice(List<Core.Models.ListingAggregate.Listing> listings)
    {
        return listings.Count > 0 ? listings.Average(l => l.Price) : 0;
    }

    private async Task<decimal> CalculateAveragePriceReductionAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken)
    {
        var priceChanges = await _context.PriceHistory
            .Where(ph => ph.TenantId == tenantId)
            .Join(
                _context.Listings.Where(l => l.DealerId == dealerId),
                ph => ph.ListingId,
                l => l.ListingId,
                (ph, l) => ph)
            .Where(ph => ph.PriceChange < 0)
            .Select(ph => ph.PriceChange!.Value)
            .ToListAsync(cancellationToken);

        return priceChanges.Count > 0 ? Math.Abs(priceChanges.Average()) : 0;
    }

    private async Task<double> CalculateAveragePriceReductionPercentAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken)
    {
        var percentChanges = await _context.PriceHistory
            .Where(ph => ph.TenantId == tenantId)
            .Join(
                _context.Listings.Where(l => l.DealerId == dealerId),
                ph => ph.ListingId,
                l => l.ListingId,
                (ph, l) => ph)
            .Where(ph => ph.ChangePercentage < 0)
            .Select(ph => (double)ph.ChangePercentage!.Value)
            .ToListAsync(cancellationToken);

        return percentChanges.Count > 0 ? Math.Abs(percentChanges.Average()) : 0;
    }

    private async Task<int> GetPriceReductionCountAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken)
    {
        return await _context.PriceHistory
            .Where(ph => ph.TenantId == tenantId)
            .Join(
                _context.Listings.Where(l => l.DealerId == dealerId),
                ph => ph.ListingId,
                l => l.ListingId,
                (ph, l) => ph)
            .Where(ph => ph.PriceChange < 0)
            .CountAsync(cancellationToken);
    }

    private static double CalculateVinProvidedRate(List<Core.Models.ListingAggregate.Listing> listings)
    {
        if (listings.Count == 0) return 0;
        return (double)listings.Count(l => !string.IsNullOrEmpty(l.Vin) && l.Vin.Length == 17) / listings.Count;
    }

    private static double CalculateImageProvidedRate(List<Core.Models.ListingAggregate.Listing> listings)
    {
        if (listings.Count == 0) return 0;
        return (double)listings.Count(l => l.ImageUrls.Count > 0) / listings.Count;
    }

    private static double CalculateDescriptionQualityScore(List<Core.Models.ListingAggregate.Listing> listings)
    {
        if (listings.Count == 0) return 0;

        var scores = listings.Select(l =>
        {
            var desc = l.Description ?? string.Empty;
            var score = 0.0;

            // Length scoring (longer descriptions generally better)
            score += Math.Min(50, desc.Length / 10.0);

            // Has key information
            if (!string.IsNullOrEmpty(l.Vin)) score += 10;
            if (l.Mileage.HasValue) score += 10;
            if (l.ImageUrls.Count > 0) score += 10;
            if (l.ImageUrls.Count > 5) score += 10;
            if (!string.IsNullOrEmpty(l.ExteriorColor)) score += 5;
            if (l.Transmission.HasValue) score += 5;

            return Math.Min(100, score);
        });

        return scores.Average();
    }
}
