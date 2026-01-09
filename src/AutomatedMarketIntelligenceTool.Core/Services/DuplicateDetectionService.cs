using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IImageHashingService? _imageHashingService;
    private readonly ILogger<DuplicateDetectionService> _logger;

    // Fuzzy matching weights
    private const double MakeModelWeight = 0.30;
    private const double YearWeight = 0.20;
    private const double MileageWeight = 0.15;
    private const double PriceWeight = 0.15;
    private const double LocationWeight = 0.10;
    private const double ImageWeight = 0.10;

    public DuplicateDetectionService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DuplicateDetectionService> logger,
        IImageHashingService? imageHashingService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageHashingService = imageHashingService;
    }

    public Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default)
    {
        return CheckForDuplicateAsync(scrapedListing, DuplicateDetectionOptions.Default, cancellationToken);
    }

    public async Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        DuplicateDetectionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (scrapedListing == null)
        {
            throw new ArgumentNullException(nameof(scrapedListing));
        }

        options ??= DuplicateDetectionOptions.Default;

        _logger.LogDebug(
            "Checking for duplicate listing: {ExternalId} from {SourceSite} with VIN {VIN}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite,
            scrapedListing.Vin ?? "N/A");

        // 1. Check VIN match (if VIN present and valid)
        if (!string.IsNullOrWhiteSpace(scrapedListing.Vin) &&
            IsValidVin(scrapedListing.Vin))
        {
            _logger.LogDebug("Checking VIN match for {VIN}", scrapedListing.Vin);

            var vinMatch = await _context.Listings
                .Where(l => l.TenantId == scrapedListing.TenantId)
                .FirstOrDefaultAsync(l =>
                    l.Vin != null &&
                    l.Vin.ToUpper() == scrapedListing.Vin.ToUpper(),
                    cancellationToken);

            if (vinMatch != null)
            {
                _logger.LogInformation(
                    "Duplicate found via VIN match: {VIN}, ExistingListingId: {ListingId}",
                    scrapedListing.Vin,
                    vinMatch.ListingId.Value);

                return DuplicateCheckResult.VinMatch(vinMatch.ListingId.Value);
            }
        }

        // 2. Check ExternalId + SourceSite match
        _logger.LogDebug(
            "Checking ExternalId match for {ExternalId} on {SourceSite}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite);

        var externalIdMatch = await _context.Listings
            .Where(l => l.TenantId == scrapedListing.TenantId)
            .FirstOrDefaultAsync(l =>
                l.ExternalId == scrapedListing.ExternalId &&
                l.SourceSite == scrapedListing.SourceSite,
                cancellationToken);

        if (externalIdMatch != null)
        {
            _logger.LogInformation(
                "Duplicate found via ExternalId match: {ExternalId} on {SourceSite}, ExistingListingId: {ListingId}",
                scrapedListing.ExternalId,
                scrapedListing.SourceSite,
                externalIdMatch.ListingId.Value);

            return DuplicateCheckResult.ExternalIdMatch(externalIdMatch.ListingId.Value);
        }

        // 3. Try fuzzy matching if enabled and we have necessary data
        if (options.EnableFuzzyMatching && HasFuzzyMatchingData(scrapedListing))
        {
            var fuzzyResult = await PerformFuzzyMatchingAsync(scrapedListing, options, cancellationToken);
            if (fuzzyResult != null)
            {
                return fuzzyResult;
            }
        }

        // 4. Try image matching if enabled
        if (options.EnableImageMatching &&
            _imageHashingService != null &&
            scrapedListing.ImageUrls?.Any() == true)
        {
            var imageResult = await PerformImageMatchingAsync(scrapedListing, options, cancellationToken);
            if (imageResult != null)
            {
                return imageResult;
            }
        }

        // 5. No match - new listing
        _logger.LogDebug(
            "No duplicate found for ExternalId: {ExternalId} from {SourceSite}",
            scrapedListing.ExternalId,
            scrapedListing.SourceSite);

        return DuplicateCheckResult.NewListing();
    }

    private async Task<DuplicateCheckResult?> PerformFuzzyMatchingAsync(
        ScrapedListingInfo scrapedListing,
        DuplicateDetectionOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing fuzzy matching for {Make} {Model} {Year}",
            scrapedListing.Make, scrapedListing.Model, scrapedListing.Year);

        // Get candidate listings with same make/model/year range
        var candidates = await _context.Listings
            .Where(l => l.TenantId == scrapedListing.TenantId)
            .Where(l => l.IsActive)
            .Where(l => l.Make.ToLower() == scrapedListing.Make!.ToLower())
            .Where(l => l.Model.ToLower() == scrapedListing.Model!.ToLower())
            .Where(l => scrapedListing.Year == null ||
                       (l.Year >= scrapedListing.Year - 1 && l.Year <= scrapedListing.Year + 1))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogDebug("No fuzzy matching candidates found");
            return null;
        }

        _logger.LogDebug("Found {Count} fuzzy matching candidates", candidates.Count);

        Listing? bestMatch = null;
        FuzzyMatchDetails? bestDetails = null;
        double bestScore = 0;

        foreach (var candidate in candidates)
        {
            var details = CalculateFuzzyScore(scrapedListing, candidate, options);
            if (details.OverallScore > bestScore)
            {
                bestScore = details.OverallScore;
                bestDetails = details;
                bestMatch = candidate;
            }
        }

        if (bestMatch == null || bestScore < options.ReviewThreshold)
        {
            _logger.LogDebug("No fuzzy match found above review threshold ({Threshold}%)", options.ReviewThreshold);
            return null;
        }

        if (bestScore >= options.AutoMatchThreshold)
        {
            _logger.LogInformation(
                "Fuzzy match found with {Score:F1}% confidence for listing {ListingId}",
                bestScore, bestMatch.ListingId.Value);

            return DuplicateCheckResult.FuzzyMatch(bestMatch.ListingId.Value, bestScore, bestDetails);
        }

        // Near-match - flag for review
        _logger.LogInformation(
            "Near-match found with {Score:F1}% confidence for listing {ListingId} - flagged for review",
            bestScore, bestMatch.ListingId.Value);

        return DuplicateCheckResult.NearMatch(
            bestMatch.ListingId.Value,
            bestScore,
            DuplicateMatchType.FuzzyMatch,
            bestDetails);
    }

    private async Task<DuplicateCheckResult?> PerformImageMatchingAsync(
        ScrapedListingInfo scrapedListing,
        DuplicateDetectionOptions options,
        CancellationToken cancellationToken)
    {
        if (_imageHashingService == null)
        {
            return null;
        }

        _logger.LogDebug("Performing image matching");

        // Get candidates with image hashes
        var candidatesQuery = _context.Listings
            .Where(l => l.TenantId == scrapedListing.TenantId)
            .Where(l => l.IsActive)
            .Where(l => l.ImageHashes != null);

        // Pre-filter by make if available
        if (!string.IsNullOrEmpty(scrapedListing.Make))
        {
            candidatesQuery = candidatesQuery.Where(l =>
                l.Make.ToLower() == scrapedListing.Make.ToLower());
        }

        // Pre-filter by year range if available
        if (scrapedListing.Year.HasValue)
        {
            candidatesQuery = candidatesQuery.Where(l =>
                l.Year >= scrapedListing.Year.Value - 2 &&
                l.Year <= scrapedListing.Year.Value + 2);
        }

        var candidates = await candidatesQuery.ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogDebug("No image matching candidates found");
            return null;
        }

        _logger.LogDebug("Found {Count} image matching candidates", candidates.Count);

        var imageResult = await _imageHashingService.FindImageMatchesAsync(
            scrapedListing.ImageUrls!,
            candidates,
            cancellationToken);

        if (!imageResult.HasMatch || imageResult.MatchedListing == null)
        {
            _logger.LogDebug("No image match found");
            return null;
        }

        // Image-only match is capped at 75% as per spec
        var confidence = Math.Min(imageResult.SimilarityScore, 75.0);

        if (confidence >= options.AutoMatchThreshold)
        {
            _logger.LogInformation(
                "Image match found with {Score:F1}% confidence for listing {ListingId}",
                confidence, imageResult.MatchedListing.ListingId.Value);

            return DuplicateCheckResult.ImageMatch(imageResult.MatchedListing.ListingId.Value, confidence);
        }

        if (confidence >= options.ReviewThreshold)
        {
            _logger.LogInformation(
                "Near image match found with {Score:F1}% confidence for listing {ListingId} - flagged for review",
                confidence, imageResult.MatchedListing.ListingId.Value);

            return DuplicateCheckResult.NearMatch(
                imageResult.MatchedListing.ListingId.Value,
                confidence,
                DuplicateMatchType.ImageMatch);
        }

        return null;
    }

    private FuzzyMatchDetails CalculateFuzzyScore(
        ScrapedListingInfo scraped,
        Listing candidate,
        DuplicateDetectionOptions options)
    {
        // Make/Model score (exact match = 100%)
        var makeModelScore = 100.0;
        if (!string.Equals(scraped.Make, candidate.Make, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(scraped.Model, candidate.Model, StringComparison.OrdinalIgnoreCase))
        {
            makeModelScore = 0;
        }

        // Year score
        double yearScore = 0;
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
        double mileageScore = 0;
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
            mileageScore = 50.0; // Neutral if both missing
        }

        // Price score
        double priceScore = 0;
        if (scraped.Price.HasValue)
        {
            var priceDiff = Math.Abs(scraped.Price.Value - candidate.Price);
            if (priceDiff <= options.PriceTolerance)
            {
                priceScore = 100.0 * (1 - (double)priceDiff / (double)options.PriceTolerance);
            }
        }

        // Location score
        double locationScore = 0;
        if (!string.IsNullOrEmpty(scraped.City) && !string.IsNullOrEmpty(candidate.City))
        {
            locationScore = string.Equals(scraped.City, candidate.City, StringComparison.OrdinalIgnoreCase)
                ? 100.0
                : 0.0;
        }
        else
        {
            locationScore = 50.0; // Neutral if location not available
        }

        // Calculate weighted overall score
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
            ImageScore = 0, // Image score added separately if image matching enabled
            OverallScore = overallScore
        };
    }

    private static bool HasFuzzyMatchingData(ScrapedListingInfo listing)
    {
        return !string.IsNullOrEmpty(listing.Make) &&
               !string.IsNullOrEmpty(listing.Model);
    }

    private static bool IsValidVin(string vin)
    {
        // VIN must be exactly 17 characters
        return vin.Length == 17;
    }
}
