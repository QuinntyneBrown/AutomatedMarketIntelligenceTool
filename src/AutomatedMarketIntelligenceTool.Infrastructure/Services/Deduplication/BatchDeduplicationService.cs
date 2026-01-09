using System.Diagnostics;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScrapedListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Deduplication;

/// <summary>
/// Service for processing batch deduplication operations with optimized
/// blocking/bucketing strategies for improved performance.
/// Target: less than 20% overhead compared to baseline.
/// </summary>
public class BatchDeduplicationService : IBatchDeduplicationService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly ILogger<BatchDeduplicationService> _logger;

    public BatchDeduplicationService(
        IAutomatedMarketIntelligenceToolContext context,
        IDuplicateDetectionService duplicateDetectionService,
        ILogger<BatchDeduplicationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _duplicateDetectionService = duplicateDetectionService ?? throw new ArgumentNullException(nameof(duplicateDetectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BatchDeduplicationResult> ProcessBatchAsync(
        IEnumerable<ScrapedListing> listings,
        Guid tenantId,
        DuplicateDetectionOptions? options = null,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var listingList = listings.ToList();
        var result = new BatchDeduplicationResult
        {
            TotalProcessed = listingList.Count
        };

        if (listingList.Count == 0)
        {
            return result;
        }

        var stopwatch = Stopwatch.StartNew();
        options ??= DuplicateDetectionOptions.Default;

        _logger.LogInformation(
            "Starting batch deduplication for {Count} listings",
            listingList.Count);

        // Step 1: Deduplicate within the batch itself
        var dedupedBatch = DeduplicateWithinBatch(listingList);
        var batchDuplicates = listingList.Count - dedupedBatch.Count;

        if (batchDuplicates > 0)
        {
            _logger.LogInformation(
                "Removed {Count} duplicates within the batch",
                batchDuplicates);
            result.DuplicateCount += batchDuplicates;
        }

        // Step 2: Pre-fetch candidate blocks for efficient matching
        var candidateIndex = await PreFetchCandidateBlocksAsync(
            dedupedBatch, tenantId, cancellationToken);

        _logger.LogInformation(
            "Pre-fetched {CandidateCount} candidates in {BlockCount} blocks",
            candidateIndex.TotalCandidates,
            candidateIndex.BlockCount);

        // Step 3: Process listings with parallel execution
        var processedCount = 0;
        var lockObject = new object();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            dedupedBatch,
            parallelOptions,
            async (listing, ct) =>
            {
                var itemStopwatch = Stopwatch.StartNew();

                try
                {
                    var scrapedInfo = ConvertToScrapedListingInfo(listing, tenantId);
                    var candidates = candidateIndex.GetCandidates(listing.Make, listing.Year);

                    var checkResult = await CheckForDuplicateWithCandidatesAsync(
                        scrapedInfo, candidates, options, ct);

                    itemStopwatch.Stop();

                    var itemResult = new BatchItemResult
                    {
                        ExternalId = listing.ExternalId ?? "unknown",
                        SourceSite = listing.SourceSite ?? "unknown",
                        Result = checkResult,
                        ProcessingTimeMs = itemStopwatch.ElapsedMilliseconds
                    };

                    lock (lockObject)
                    {
                        result.ItemResults.Add(itemResult);
                        UpdateResultCounts(result, checkResult);

                        processedCount++;
                        if (processedCount % 100 == 0 || processedCount == dedupedBatch.Count)
                        {
                            progress?.Report(new BatchProgress(processedCount, dedupedBatch.Count)
                            {
                                Message = $"Processed {processedCount}/{dedupedBatch.Count} listings"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        result.Errors.Add($"Error processing {listing.ExternalId}: {ex.Message}");
                    }

                    _logger.LogWarning(ex,
                        "Error processing listing {ExternalId} from {SourceSite}",
                        listing.ExternalId, listing.SourceSite);
                }
            });

        stopwatch.Stop();
        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Batch deduplication completed. New: {New}, Duplicates: {Dup}, Near-matches: {Near}, Time: {Time}ms, Avg: {Avg:F2}ms/listing",
            result.NewListingCount,
            result.DuplicateCount,
            result.NearMatchCount,
            result.ProcessingTimeMs,
            result.AverageTimePerListingMs);

        return result;
    }

    public async Task<CandidateBlockIndex> PreFetchCandidateBlocksAsync(
        IEnumerable<ScrapedListing> listings,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var listingList = listings.ToList();

        // Get unique make/year combinations from input
        var makeYears = listingList
            .Where(l => !string.IsNullOrEmpty(l.Make))
            .Select(l => (Make: l.Make!.ToUpperInvariant(), Year: l.Year))
            .Distinct()
            .ToList();

        if (makeYears.Count == 0)
        {
            return new CandidateBlockIndex();
        }

        // Build predicate for blocking query - fetch candidates by make + year range (±2 years)
        var makes = makeYears.Select(my => my.Make).Distinct().ToList();
        var minYear = makeYears.Min(my => my.Year) - 2;
        var maxYear = makeYears.Max(my => my.Year) + 2;

        var candidates = await _context.Listings
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .Where(l => l.IsActive)
            .Where(l => makes.Contains(l.Make.ToUpper()))
            .Where(l => l.Year >= minYear && l.Year <= maxYear)
            .Select(l => new CandidateListing
            {
                ListingId = l.ListingId.Value,
                Make = l.Make,
                Model = l.Model,
                Year = l.Year,
                Price = l.Price,
                Mileage = l.Mileage,
                Vin = l.Vin,
                City = l.City,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                ExternalId = l.ExternalId,
                SourceSite = l.SourceSite,
                LinkedVehicleId = l.LinkedVehicleId
            })
            .ToListAsync(cancellationToken);

        return new CandidateBlockIndex(candidates);
    }

    private async Task<DuplicateCheckResult> CheckForDuplicateWithCandidatesAsync(
        ScrapedListingInfo scrapedInfo,
        IEnumerable<CandidateListing> candidates,
        DuplicateDetectionOptions options,
        CancellationToken cancellationToken)
    {
        var candidateList = candidates.ToList();

        // 1. Check VIN match
        if (!string.IsNullOrWhiteSpace(scrapedInfo.Vin) && scrapedInfo.Vin.Length == 17)
        {
            var vinMatch = candidateList.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.Vin) &&
                c.Vin.Equals(scrapedInfo.Vin, StringComparison.OrdinalIgnoreCase));

            if (vinMatch != null)
            {
                return DuplicateCheckResult.VinMatch(vinMatch.ListingId);
            }
        }

        // 2. Check ExternalId + SourceSite match
        var externalIdMatch = candidateList.FirstOrDefault(c =>
            c.ExternalId == scrapedInfo.ExternalId &&
            c.SourceSite == scrapedInfo.SourceSite);

        if (externalIdMatch != null)
        {
            return DuplicateCheckResult.ExternalIdMatch(externalIdMatch.ListingId);
        }

        // 3. Fuzzy matching if enabled
        if (options.EnableFuzzyMatching && HasFuzzyMatchingData(scrapedInfo))
        {
            var fuzzyResult = PerformFuzzyMatching(scrapedInfo, candidateList, options);
            if (fuzzyResult != null)
            {
                return fuzzyResult;
            }
        }

        // 4. Fall back to full service check for image matching
        if (options.EnableImageMatching && scrapedInfo.ImageUrls?.Any() == true)
        {
            return await _duplicateDetectionService.CheckForDuplicateAsync(
                scrapedInfo, options, cancellationToken);
        }

        return DuplicateCheckResult.NewListing();
    }

    private DuplicateCheckResult? PerformFuzzyMatching(
        ScrapedListingInfo scraped,
        List<CandidateListing> candidates,
        DuplicateDetectionOptions options)
    {
        CandidateListing? bestMatch = null;
        FuzzyMatchDetails? bestDetails = null;
        double bestScore = 0;

        foreach (var candidate in candidates)
        {
            // Skip if same external ID (already checked)
            if (candidate.ExternalId == scraped.ExternalId &&
                candidate.SourceSite == scraped.SourceSite)
            {
                continue;
            }

            var details = CalculateFuzzyScore(scraped, candidate, options);
            if (details.OverallScore > bestScore)
            {
                bestScore = details.OverallScore;
                bestDetails = details;
                bestMatch = candidate;
            }
        }

        if (bestMatch == null || bestScore < options.ReviewThreshold)
        {
            return null;
        }

        if (bestScore >= options.AutoMatchThreshold)
        {
            return DuplicateCheckResult.FuzzyMatch(bestMatch.ListingId, bestScore, bestDetails);
        }

        // Near-match - flag for review
        return DuplicateCheckResult.NearMatch(
            bestMatch.ListingId,
            bestScore,
            DuplicateMatchType.FuzzyMatch,
            bestDetails);
    }

    private static FuzzyMatchDetails CalculateFuzzyScore(
        ScrapedListingInfo scraped,
        CandidateListing candidate,
        DuplicateDetectionOptions options)
    {
        const double MakeModelWeight = 0.30;
        const double YearWeight = 0.20;
        const double MileageWeight = 0.15;
        const double PriceWeight = 0.15;
        const double LocationWeight = 0.20;

        // Make/Model score
        var makeModelScore = 0.0;
        if (string.Equals(scraped.Make, candidate.Make, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(scraped.Model, candidate.Model, StringComparison.OrdinalIgnoreCase))
        {
            makeModelScore = 100.0;
        }

        // Year score
        var yearScore = 0.0;
        if (scraped.Year.HasValue)
        {
            var yearDiff = Math.Abs(scraped.Year.Value - candidate.Year);
            yearScore = yearDiff switch
            {
                0 => 100.0,
                1 => 75.0,
                2 => 50.0,
                _ => 0.0
            };
        }

        // Mileage score
        var mileageScore = 0.0;
        if (scraped.Mileage.HasValue && candidate.Mileage.HasValue)
        {
            var mileageDiff = Math.Abs(scraped.Mileage.Value - candidate.Mileage.Value);
            if (mileageDiff <= options.MileageTolerance)
            {
                mileageScore = 100.0 * (1 - (double)mileageDiff / options.MileageTolerance);
            }
        }
        else if (!scraped.Mileage.HasValue && !candidate.Mileage.HasValue)
        {
            mileageScore = 50.0;
        }

        // Price score
        var priceScore = 0.0;
        if (scraped.Price.HasValue)
        {
            var priceDiff = Math.Abs(scraped.Price.Value - candidate.Price);
            if (priceDiff <= options.PriceTolerance)
            {
                priceScore = 100.0 * (1 - (double)priceDiff / (double)options.PriceTolerance);
            }
        }

        // Location score
        var locationScore = 0.0;
        if (!string.IsNullOrEmpty(scraped.City) && !string.IsNullOrEmpty(candidate.City))
        {
            locationScore = string.Equals(scraped.City, candidate.City, StringComparison.OrdinalIgnoreCase)
                ? 100.0
                : 0.0;
        }
        else
        {
            locationScore = 50.0;
        }

        var overallScore =
            makeModelScore * MakeModelWeight +
            yearScore * YearWeight +
            mileageScore * MileageWeight +
            priceScore * PriceWeight +
            locationScore * LocationWeight;

        return new FuzzyMatchDetails
        {
            MakeModelScore = makeModelScore,
            YearScore = yearScore,
            MileageScore = mileageScore,
            PriceScore = priceScore,
            LocationScore = locationScore,
            ImageScore = 0,
            OverallScore = overallScore
        };
    }

    private static List<ScrapedListing> DeduplicateWithinBatch(List<ScrapedListing> listings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ScrapedListing>();

        foreach (var listing in listings)
        {
            // Create a unique key based on VIN or ExternalId+SourceSite
            var key = !string.IsNullOrWhiteSpace(listing.Vin)
                ? $"VIN:{listing.Vin}"
                : $"{listing.SourceSite}:{listing.ExternalId}";

            if (seen.Add(key))
            {
                result.Add(listing);
            }
        }

        return result;
    }

    private static ScrapedListingInfo ConvertToScrapedListingInfo(ScrapedListing listing, Guid tenantId)
    {
        return new ScrapedListingInfo
        {
            ExternalId = listing.ExternalId ?? string.Empty,
            SourceSite = listing.SourceSite ?? string.Empty,
            Vin = listing.Vin,
            TenantId = tenantId,
            Make = listing.Make,
            Model = listing.Model,
            Year = listing.Year,
            Mileage = listing.Mileage,
            Price = listing.Price,
            City = listing.City,
            ImageUrls = listing.ImageUrls
        };
    }

    private static bool HasFuzzyMatchingData(ScrapedListingInfo listing)
    {
        return !string.IsNullOrEmpty(listing.Make) &&
               !string.IsNullOrEmpty(listing.Model);
    }

    private static void UpdateResultCounts(BatchDeduplicationResult result, DuplicateCheckResult checkResult)
    {
        if (checkResult.IsDuplicate)
        {
            result.DuplicateCount++;

            switch (checkResult.MatchType)
            {
                case DuplicateMatchType.VinMatch:
                case DuplicateMatchType.ExternalIdMatch:
                    result.VinMatchCount++;
                    break;
                case DuplicateMatchType.FuzzyMatch:
                    result.FuzzyMatchCount++;
                    break;
                case DuplicateMatchType.ImageMatch:
                    result.ImageMatchCount++;
                    break;
            }
        }
        else if (checkResult.RequiresReview)
        {
            result.NearMatchCount++;
        }
        else
        {
            result.NewListingCount++;
        }
    }
}
