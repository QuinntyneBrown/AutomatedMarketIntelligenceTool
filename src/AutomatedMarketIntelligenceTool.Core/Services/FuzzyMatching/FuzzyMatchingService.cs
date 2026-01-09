using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Service for fuzzy matching of vehicle listings across sources.
/// </summary>
public class FuzzyMatchingService : IFuzzyMatchingService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ConfidenceScoreCalculator _confidenceCalculator;
    private readonly ILogger<FuzzyMatchingService> _logger;
    private const decimal DefaultThreshold = 85m;
    private const int PartialVinLength = 8;

    public FuzzyMatchingService(
        IAutomatedMarketIntelligenceToolContext context,
        ConfidenceScoreCalculator confidenceCalculator,
        ILogger<FuzzyMatchingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _confidenceCalculator = confidenceCalculator ?? throw new ArgumentNullException(nameof(confidenceCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<FuzzyMatchResult> FindBestMatchAsync(
        ScrapedListingData listing,
        CancellationToken cancellationToken = default)
    {
        if (listing == null)
        {
            throw new ArgumentNullException(nameof(listing));
        }

        _logger.LogDebug(
            "Starting fuzzy match for {Make} {Model} {Year} from {SourceSite}",
            listing.Make,
            listing.Model,
            listing.Year,
            listing.SourceSite);

        // 1. Check for exact VIN match (full 17 characters)
        if (!string.IsNullOrWhiteSpace(listing.Vin) && listing.Vin.Length == 17)
        {
            var exactVinMatch = await CheckExactVinMatchAsync(listing, cancellationToken);
            if (exactVinMatch != null)
            {
                _logger.LogInformation("Exact VIN match found for {VIN}", listing.Vin);
                return new FuzzyMatchResult
                {
                    MatchedListing = exactVinMatch,
                    ConfidenceScore = 100m,
                    Method = MatchMethod.ExactVin,
                    FieldScores = new Dictionary<string, decimal>()
                };
            }
        }

        // 2. Check for partial VIN match (last 8 characters)
        if (!string.IsNullOrWhiteSpace(listing.Vin) && listing.Vin.Length >= PartialVinLength)
        {
            var partialVinMatch = await CheckPartialVinMatchAsync(listing, cancellationToken);
            if (partialVinMatch != null)
            {
                _logger.LogInformation("Partial VIN match found for {VIN}", listing.Vin);
                return new FuzzyMatchResult
                {
                    MatchedListing = partialVinMatch,
                    ConfidenceScore = 95m,
                    Method = MatchMethod.PartialVin,
                    FieldScores = new Dictionary<string, decimal>()
                };
            }
        }

        // 3. Check for ExternalId + SourceSite match
        var externalIdMatch = await CheckExternalIdMatchAsync(listing, cancellationToken);
        if (externalIdMatch != null)
        {
            _logger.LogInformation(
                "ExternalId match found for {ExternalId} on {SourceSite}",
                listing.ExternalId,
                listing.SourceSite);
            return new FuzzyMatchResult
            {
                MatchedListing = externalIdMatch,
                ConfidenceScore = 100m,
                Method = MatchMethod.ExternalId,
                FieldScores = new Dictionary<string, decimal>()
            };
        }

        // 4. Perform fuzzy attribute matching
        var fuzzyMatch = await FindBestFuzzyMatchAsync(listing, cancellationToken);
        
        if (fuzzyMatch != null && fuzzyMatch.ConfidenceScore >= DefaultThreshold)
        {
            _logger.LogInformation(
                "Fuzzy match found with {Confidence}% confidence for {Make} {Model} {Year}",
                fuzzyMatch.ConfidenceScore,
                listing.Make,
                listing.Model,
                listing.Year);
            return fuzzyMatch;
        }

        _logger.LogDebug("No match found for {Make} {Model} {Year}", listing.Make, listing.Model, listing.Year);
        return new FuzzyMatchResult
        {
            MatchedListing = null,
            ConfidenceScore = 0m,
            Method = MatchMethod.None,
            FieldScores = new Dictionary<string, decimal>()
        };
    }

    private async Task<Listing?> CheckExactVinMatchAsync(
        ScrapedListingData listing,
        CancellationToken cancellationToken)
    {
        return await _context.Listings
            .Where(l => l.TenantId == listing.TenantId)
            .FirstOrDefaultAsync(
                l => l.Vin != null && l.Vin.ToUpper() == listing.Vin!.ToUpper(),
                cancellationToken);
    }

    private async Task<Listing?> CheckPartialVinMatchAsync(
        ScrapedListingData listing,
        CancellationToken cancellationToken)
    {
        var last8 = listing.Vin!.Substring(listing.Vin.Length - PartialVinLength).ToUpper();
        
        var listings = await _context.Listings
            .Where(l => l.TenantId == listing.TenantId)
            .Where(l => l.Vin != null && l.Vin.Length >= PartialVinLength)
            .ToListAsync(cancellationToken);

        return listings.FirstOrDefault(l => 
            l.Vin!.Substring(l.Vin.Length - PartialVinLength).ToUpper() == last8);
    }

    private async Task<Listing?> CheckExternalIdMatchAsync(
        ScrapedListingData listing,
        CancellationToken cancellationToken)
    {
        return await _context.Listings
            .Where(l => l.TenantId == listing.TenantId)
            .FirstOrDefaultAsync(
                l => l.ExternalId == listing.ExternalId && l.SourceSite == listing.SourceSite,
                cancellationToken);
    }

    private async Task<FuzzyMatchResult?> FindBestFuzzyMatchAsync(
        ScrapedListingData listing,
        CancellationToken cancellationToken)
    {
        // Get candidates: same make and model, within reasonable year range
        var candidates = await _context.Listings
            .Where(l => l.TenantId == listing.TenantId)
            .Where(l => l.IsActive)
            .Where(l => l.Make == listing.Make)
            .Where(l => l.Model == listing.Model)
            .Where(l => Math.Abs(l.Year - listing.Year) <= 2)
            .ToListAsync(cancellationToken);

        if (!candidates.Any())
        {
            return null;
        }

        _logger.LogDebug("Evaluating {Count} candidate listings for fuzzy match", candidates.Count);

        // Calculate confidence score for each candidate
        var results = candidates
            .Select(c => _confidenceCalculator.Calculate(c, listing))
            .Where(r => r.ConfidenceScore >= DefaultThreshold)
            .OrderByDescending(r => r.ConfidenceScore)
            .ToList();

        return results.FirstOrDefault();
    }
}
