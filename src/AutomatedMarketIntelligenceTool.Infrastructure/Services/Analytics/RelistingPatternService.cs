using System.Diagnostics;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;

/// <summary>
/// Service for detecting and tracking relisting patterns.
/// </summary>
public class RelistingPatternService : IRelistingPatternService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<RelistingPatternService> _logger;

    private const double VinMatchConfidence = 100.0;
    private const double ExternalIdMatchConfidence = 95.0;
    private const double FuzzyMatchMinConfidence = 70.0;

    public RelistingPatternService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<RelistingPatternService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RelistingDetectionResult> DetectRelistingAsync(
        Guid tenantId,
        Listing currentListing,
        CancellationToken cancellationToken = default)
    {
        var result = new RelistingDetectionResult { IsRelisting = false };

        // Get recently deactivated listings to check against
        var lookbackDate = DateTime.UtcNow.AddDays(-90);
        var candidates = await _context.Listings
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .Where(l => !l.IsActive)
            .Where(l => l.DeactivatedAt >= lookbackDate)
            .Where(l => l.ListingId != currentListing.ListingId)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return result;
        }

        // 1. Check VIN match
        if (!string.IsNullOrWhiteSpace(currentListing.Vin) && currentListing.Vin.Length == 17)
        {
            var vinMatch = candidates.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.Vin) &&
                c.Vin.Equals(currentListing.Vin, StringComparison.OrdinalIgnoreCase));

            if (vinMatch != null)
            {
                var pattern = CreateAndSavePattern(
                    tenantId, currentListing, vinMatch,
                    RelistingType.VinMatch, VinMatchConfidence, "VIN");

                await _context.SaveChangesAsync(cancellationToken);

                return new RelistingDetectionResult
                {
                    IsRelisting = true,
                    Pattern = pattern,
                    MatchedListingId = vinMatch.ListingId,
                    MatchConfidence = VinMatchConfidence,
                    MatchMethod = "VIN"
                };
            }
        }

        // 2. Check ExternalId + SourceSite match
        var externalIdMatch = candidates.FirstOrDefault(c =>
            c.ExternalId == currentListing.ExternalId &&
            c.SourceSite == currentListing.SourceSite);

        if (externalIdMatch != null)
        {
            var pattern = CreateAndSavePattern(
                tenantId, currentListing, externalIdMatch,
                RelistingType.ExternalIdMatch, ExternalIdMatchConfidence, "ExternalId");

            await _context.SaveChangesAsync(cancellationToken);

            return new RelistingDetectionResult
            {
                IsRelisting = true,
                Pattern = pattern,
                MatchedListingId = externalIdMatch.ListingId,
                MatchConfidence = ExternalIdMatchConfidence,
                MatchMethod = "ExternalId"
            };
        }

        // 3. Check fuzzy match for same dealer
        if (currentListing.DealerId != null)
        {
            var dealerCandidates = candidates
                .Where(c => c.DealerId == currentListing.DealerId)
                .ToList();

            foreach (var candidate in dealerCandidates)
            {
                var confidence = CalculateFuzzyMatchConfidence(currentListing, candidate);
                if (confidence >= FuzzyMatchMinConfidence)
                {
                    var pattern = CreateAndSavePattern(
                        tenantId, currentListing, candidate,
                        RelistingType.FuzzyMatch, confidence, "FuzzyMatch");

                    await _context.SaveChangesAsync(cancellationToken);

                    return new RelistingDetectionResult
                    {
                        IsRelisting = true,
                        Pattern = pattern,
                        MatchedListingId = candidate.ListingId,
                        MatchConfidence = confidence,
                        MatchMethod = "FuzzyMatch"
                    };
                }
            }
        }

        return result;
    }

    public async Task<List<RelistingPattern>> GetDealerRelistingPatternsAsync(
        Guid tenantId,
        DealerId dealerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RelistingPatterns
            .Where(p => p.TenantId == tenantId && p.DealerId == dealerId);

        if (fromDate.HasValue)
            query = query.Where(p => p.DetectedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.DetectedAt <= toDate.Value);

        query = query.OrderByDescending(p => p.DetectedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<RelistingPattern>> GetListingRelistingHistoryAsync(
        Guid tenantId,
        ListingId listingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RelistingPatterns
            .Where(p => p.TenantId == tenantId)
            .Where(p => p.CurrentListingId == listingId || p.PreviousListingId == listingId)
            .OrderByDescending(p => p.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RelistingPattern>> GetSuspiciousPatternsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RelistingPatterns
            .Where(p => p.TenantId == tenantId && p.IsSuspiciousPattern);

        if (fromDate.HasValue)
            query = query.Where(p => p.DetectedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.DetectedAt <= toDate.Value);

        query = query.OrderByDescending(p => p.DetectedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<RelistingScanResult> ScanForRelistingsAsync(
        Guid tenantId,
        int lookbackDays = 30,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new RelistingScanResult();

        var fromDate = DateTime.UtcNow.AddDays(-lookbackDays);

        // Get recent active listings
        var activeListings = await _context.Listings
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .Where(l => l.IsActive)
            .Where(l => l.CreatedAt >= fromDate)
            .ToListAsync(cancellationToken);

        result.ListingsScanned = activeListings.Count;

        _logger.LogInformation(
            "Scanning {Count} listings for relisting patterns",
            activeListings.Count);

        for (var i = 0; i < activeListings.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var listing = activeListings[i];

            // Check if we already have a pattern for this listing
            var existingPattern = await _context.RelistingPatterns
                .AnyAsync(p => p.TenantId == tenantId && p.CurrentListingId == listing.ListingId, cancellationToken);

            if (!existingPattern)
            {
                var detectionResult = await DetectRelistingAsync(tenantId, listing, cancellationToken);
                if (detectionResult.IsRelisting && detectionResult.Pattern != null)
                {
                    result.RelistingsFound++;
                    result.Patterns.Add(detectionResult.Pattern);

                    if (detectionResult.Pattern.IsSuspiciousPattern)
                    {
                        result.SuspiciousPatterns++;
                    }
                }
            }

            if ((i + 1) % 50 == 0 || i == activeListings.Count - 1)
            {
                progress?.Report(new BatchProgress(i + 1, activeListings.Count,
                    $"Scanned {i + 1}/{activeListings.Count} listings"));
            }
        }

        stopwatch.Stop();
        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Relisting scan completed. Scanned: {Scanned}, Found: {Found}, Suspicious: {Suspicious}, Time: {Time}ms",
            result.ListingsScanned, result.RelistingsFound, result.SuspiciousPatterns, result.ProcessingTimeMs);

        return result;
    }

    public async Task<DealerRelistingStatistics> GetDealerRelistingStatisticsAsync(
        Guid tenantId,
        DealerId dealerId,
        CancellationToken cancellationToken = default)
    {
        var patterns = await _context.RelistingPatterns
            .Where(p => p.TenantId == tenantId && p.DealerId == dealerId)
            .ToListAsync(cancellationToken);

        var totalListings = await _context.Listings
            .CountAsync(l => l.TenantId == tenantId && l.DealerId == dealerId, cancellationToken);

        var stats = new DealerRelistingStatistics
        {
            DealerId = dealerId,
            TotalListings = totalListings,
            TotalRelistings = patterns.Count,
            RelistingRate = totalListings > 0 ? (double)patterns.Count / totalListings : 0,
            SuspiciousRelistings = patterns.Count(p => p.IsSuspiciousPattern),
            AverageDaysBetweenRelistings = patterns.Count > 0
                ? patterns.Average(p => p.DaysBetweenListings)
                : 0,
            AveragePriceChangeOnRelist = patterns.Count > 0 && patterns.Any(p => p.PriceChange.HasValue)
                ? patterns.Where(p => p.PriceChange.HasValue).Average(p => p.PriceChange!.Value)
                : 0,
            AveragePriceChangePercentOnRelist = patterns.Count > 0 && patterns.Any(p => p.PriceChangePercent.HasValue)
                ? patterns.Where(p => p.PriceChangePercent.HasValue).Average(p => p.PriceChangePercent!.Value)
                : 0
        };

        // Check if dealer should be flagged as frequent relister
        stats.IsFrequentRelister = stats.RelistingRate >= 0.20 && stats.TotalRelistings >= 5;

        return stats;
    }

    public async Task<MarketRelistingStatistics> GetMarketRelistingStatisticsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var patternsQuery = _context.RelistingPatterns
            .Where(p => p.TenantId == tenantId);

        if (fromDate.HasValue)
            patternsQuery = patternsQuery.Where(p => p.DetectedAt >= fromDate.Value);

        if (toDate.HasValue)
            patternsQuery = patternsQuery.Where(p => p.DetectedAt <= toDate.Value);

        var patterns = await patternsQuery.ToListAsync(cancellationToken);

        var totalListings = await _context.Listings
            .CountAsync(l => l.TenantId == tenantId, cancellationToken);

        var frequentRelisters = await _context.Dealers
            .CountAsync(d => d.TenantId == tenantId && d.FrequentRelisterFlag, cancellationToken);

        var topRelistingDealers = patterns
            .Where(p => p.DealerId != null)
            .GroupBy(p => p.DealerId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return new MarketRelistingStatistics
        {
            TotalListings = totalListings,
            TotalRelistings = patterns.Count,
            MarketRelistingRate = totalListings > 0 ? (double)patterns.Count / totalListings : 0,
            SuspiciousRelistings = patterns.Count(p => p.IsSuspiciousPattern),
            FrequentRelisterCount = frequentRelisters,
            AverageDaysBetweenRelistings = patterns.Count > 0
                ? patterns.Average(p => p.DaysBetweenListings)
                : 0,
            AveragePriceChangeOnRelist = patterns.Count > 0 && patterns.Any(p => p.PriceChange.HasValue)
                ? patterns.Where(p => p.PriceChange.HasValue).Average(p => p.PriceChange!.Value)
                : 0,
            CountByType = patterns.GroupBy(p => p.Type).ToDictionary(g => g.Key, g => g.Count()),
            TopRelistingDealers = topRelistingDealers
        };
    }

    public async Task<int> UpdateFrequentRelisterFlagsAsync(
        Guid tenantId,
        double relistingRateThreshold = 0.20,
        int minRelistings = 5,
        CancellationToken cancellationToken = default)
    {
        var dealers = await _context.Dealers
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var updatedCount = 0;

        foreach (var dealer in dealers)
        {
            var stats = await GetDealerRelistingStatisticsAsync(tenantId, dealer.DealerId, cancellationToken);

            var shouldBeFrequentRelister =
                stats.RelistingRate >= relistingRateThreshold &&
                stats.TotalRelistings >= minRelistings;

            if (dealer.FrequentRelisterFlag != shouldBeFrequentRelister)
            {
                dealer.SetFrequentRelisterFlag(shouldBeFrequentRelister);
                updatedCount++;

                _logger.LogInformation(
                    "Updated dealer {DealerId} frequent relister flag to {Flag}",
                    dealer.DealerId.Value, shouldBeFrequentRelister);
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Updated frequent relister flags for {Count} dealers",
            updatedCount);

        return updatedCount;
    }

    public async Task<RelistingPattern> RecordManualRelistingAsync(
        Guid tenantId,
        ListingId currentListingId,
        ListingId previousListingId,
        CancellationToken cancellationToken = default)
    {
        var currentListing = await _context.Listings
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.ListingId == currentListingId, cancellationToken)
            ?? throw new ArgumentException($"Current listing {currentListingId.Value} not found");

        var previousListing = await _context.Listings
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.ListingId == previousListingId, cancellationToken)
            ?? throw new ArgumentException($"Previous listing {previousListingId.Value} not found");

        var pattern = RelistingPattern.Create(
            tenantId,
            currentListingId,
            previousListingId,
            currentListing.DealerId,
            RelistingType.CombinedMatch,
            100.0, // Manual = 100% confidence
            "Manual",
            previousListing.Price,
            currentListing.Price,
            previousListing.DeactivatedAt ?? previousListing.LastSeenDate,
            currentListing.FirstSeenDate,
            previousListing.DaysOnMarket ?? 0,
            currentListing.Vin,
            currentListing.Make,
            currentListing.Model,
            currentListing.Year);

        _context.RelistingPatterns.Add(pattern);

        // Update listing relisting count
        currentListing.MarkAsRelisted(previousListingId.Value);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded manual relisting pattern: {Current} <- {Previous}",
            currentListingId.Value, previousListingId.Value);

        return pattern;
    }

    private RelistingPattern CreateAndSavePattern(
        Guid tenantId,
        Listing currentListing,
        Listing previousListing,
        RelistingType type,
        double confidence,
        string matchMethod)
    {
        var pattern = RelistingPattern.Create(
            tenantId,
            currentListing.ListingId,
            previousListing.ListingId,
            currentListing.DealerId,
            type,
            confidence,
            matchMethod,
            previousListing.Price,
            currentListing.Price,
            previousListing.DeactivatedAt ?? previousListing.LastSeenDate,
            currentListing.FirstSeenDate,
            previousListing.DaysOnMarket ?? 0,
            currentListing.Vin,
            currentListing.Make,
            currentListing.Model,
            currentListing.Year);

        _context.RelistingPatterns.Add(pattern);

        // Update listing
        currentListing.MarkAsRelisted(previousListing.ListingId.Value);

        return pattern;
    }

    private static double CalculateFuzzyMatchConfidence(Listing current, Listing candidate)
    {
        var score = 0.0;
        var weightTotal = 0.0;

        // Make/Model match (30%)
        const double makeModelWeight = 0.30;
        if (string.Equals(current.Make, candidate.Make, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(current.Model, candidate.Model, StringComparison.OrdinalIgnoreCase))
        {
            score += 100 * makeModelWeight;
        }
        weightTotal += makeModelWeight;

        // Year match (20%)
        const double yearWeight = 0.20;
        if (current.Year == candidate.Year)
        {
            score += 100 * yearWeight;
        }
        else if (Math.Abs(current.Year - candidate.Year) == 1)
        {
            score += 50 * yearWeight;
        }
        weightTotal += yearWeight;

        // Mileage proximity (20%)
        const double mileageWeight = 0.20;
        if (current.Mileage.HasValue && candidate.Mileage.HasValue)
        {
            var mileageDiff = Math.Abs(current.Mileage.Value - candidate.Mileage.Value);
            if (mileageDiff <= 1000)
            {
                score += (100 - mileageDiff / 10.0) * mileageWeight;
            }
        }
        else
        {
            score += 50 * mileageWeight; // Unknown mileage - neutral
        }
        weightTotal += mileageWeight;

        // Price proximity (15%)
        const double priceWeight = 0.15;
        var priceDiff = Math.Abs(current.Price - candidate.Price);
        if (priceDiff <= 2000)
        {
            score += (100 - (double)priceDiff / 20) * priceWeight;
        }
        weightTotal += priceWeight;

        // Color match (15%)
        const double colorWeight = 0.15;
        if (!string.IsNullOrEmpty(current.ExteriorColor) &&
            string.Equals(current.ExteriorColor, candidate.ExteriorColor, StringComparison.OrdinalIgnoreCase))
        {
            score += 100 * colorWeight;
        }
        else
        {
            score += 50 * colorWeight; // Unknown color - neutral
        }
        weightTotal += colorWeight;

        return weightTotal > 0 ? score / weightTotal * 100 : 0;
    }
}
