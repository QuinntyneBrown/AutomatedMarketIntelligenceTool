using Deduplication.Core.Entities;
using Deduplication.Core.Interfaces;
using Deduplication.Core.Interfaces.Calculators;
using Deduplication.Core.Models;

namespace Deduplication.Infrastructure.Services;

public sealed class FuzzyMatchingService : IFuzzyMatchingService
{
    private readonly IStringDistanceCalculator _stringCalculator;
    private readonly INumericProximityCalculator _numericCalculator;
    private readonly ILocationProximityCalculator _locationCalculator;
    private readonly IImageHashComparer _imageComparer;

    public FuzzyMatchingService(
        IStringDistanceCalculator stringCalculator,
        INumericProximityCalculator numericCalculator,
        ILocationProximityCalculator locationCalculator,
        IImageHashComparer imageComparer)
    {
        _stringCalculator = stringCalculator;
        _numericCalculator = numericCalculator;
        _locationCalculator = locationCalculator;
        _imageComparer = imageComparer;
    }

    public MatchResult CalculateMatchScore(
        ListingData source,
        ListingData target,
        DeduplicationConfig config)
    {
        // VIN matching - exact match is very strong indicator
        var vinScore = CalculateVinScore(source.VIN, target.VIN);

        // Title similarity using multiple algorithms
        var titleScore = CalculateTitleScore(source, target);

        // Price similarity
        var priceScore = _numericCalculator.CalculatePriceSimilarity(source.Price, target.Price);

        // Mileage similarity
        var mileageScore = _numericCalculator.CalculateMileageSimilarity(source.Mileage, target.Mileage);

        // Location similarity
        var locationScore = _locationCalculator.CalculateCombinedLocationSimilarity(
            source.Latitude, source.Longitude, source.City, source.Province, source.PostalCode,
            target.Latitude, target.Longitude, target.City, target.Province, target.PostalCode);

        // Image hash similarity
        var imageScore = _imageComparer.CalculateHashSimilarity(source.ImageHash, target.ImageHash);

        // Calculate weighted overall score
        var totalWeight = config.VinWeight + config.TitleWeight + config.PriceWeight +
                          config.MileageWeight + config.LocationWeight + config.ImageHashWeight;

        var overallScore = (
            vinScore * config.VinWeight +
            titleScore * config.TitleWeight +
            priceScore * config.PriceWeight +
            mileageScore * config.MileageWeight +
            locationScore * config.LocationWeight +
            imageScore * config.ImageHashWeight
        ) / totalWeight;

        // Boost score if VIN matches exactly
        if (vinScore >= 1.0)
        {
            overallScore = Math.Max(overallScore, 0.95);
        }

        return new MatchResult
        {
            SourceId = source.Id,
            TargetId = target.Id,
            OverallScore = overallScore,
            VinScore = vinScore,
            TitleScore = titleScore,
            PriceScore = priceScore,
            LocationScore = locationScore,
            ImageScore = imageScore,
            IsAboveThreshold = overallScore >= config.OverallMatchThreshold,
            RequiresReview = overallScore >= config.ReviewThreshold && overallScore < config.OverallMatchThreshold
        };
    }

    public Task<IReadOnlyList<MatchResult>> FindPotentialMatchesAsync(
        ListingData listing,
        IEnumerable<ListingData> candidates,
        DeduplicationConfig config,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MatchResult>();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (candidate.Id == listing.Id)
                continue;

            // Quick pre-filter: skip if different make/model/year (when available)
            if (!PassesPreFilter(listing, candidate))
                continue;

            var result = CalculateMatchScore(listing, candidate, config);

            if (result.IsAboveThreshold || result.RequiresReview)
            {
                results.Add(result);
            }
        }

        return Task.FromResult<IReadOnlyList<MatchResult>>(
            results.OrderByDescending(r => r.OverallScore).ToList());
    }

    private double CalculateVinScore(string? vin1, string? vin2)
    {
        if (string.IsNullOrWhiteSpace(vin1) || string.IsNullOrWhiteSpace(vin2))
            return 0.5; // Neutral when VIN not available

        var normalized1 = NormalizeVin(vin1);
        var normalized2 = NormalizeVin(vin2);

        if (normalized1 == normalized2)
            return 1.0;

        // Check for partial VIN match (some sources may truncate)
        if (normalized1.Length >= 8 && normalized2.Length >= 8)
        {
            // Last 8 characters of VIN are unique
            var suffix1 = normalized1.Length >= 8 ? normalized1[^8..] : normalized1;
            var suffix2 = normalized2.Length >= 8 ? normalized2[^8..] : normalized2;
            if (suffix1 == suffix2)
                return 0.9;
        }

        // Very similar VINs might have OCR errors
        var similarity = _stringCalculator.CalculateLevenshteinSimilarity(normalized1, normalized2);
        return similarity > 0.9 ? similarity : 0.0;
    }

    private double CalculateTitleScore(ListingData source, ListingData target)
    {
        // Use vehicle attributes when available
        var attributeScore = CalculateVehicleAttributeScore(source, target);

        // Title string similarity
        var titleSimilarity = _stringCalculator.CalculateJaroWinklerSimilarity(source.Title, target.Title);
        var ngramSimilarity = _stringCalculator.CalculateNGramSimilarity(source.Title, target.Title, 3);

        // Combine scores
        if (attributeScore > 0)
        {
            return attributeScore * 0.6 + titleSimilarity * 0.25 + ngramSimilarity * 0.15;
        }

        return titleSimilarity * 0.6 + ngramSimilarity * 0.4;
    }

    private static double CalculateVehicleAttributeScore(ListingData source, ListingData target)
    {
        var scores = new List<double>();

        // Year match
        if (source.Year.HasValue && target.Year.HasValue)
        {
            scores.Add(source.Year.Value == target.Year.Value ? 1.0 : 0.0);
        }

        // Make match (case-insensitive)
        if (!string.IsNullOrWhiteSpace(source.Make) && !string.IsNullOrWhiteSpace(target.Make))
        {
            scores.Add(source.Make.Equals(target.Make, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);
        }

        // Model match (case-insensitive)
        if (!string.IsNullOrWhiteSpace(source.Model) && !string.IsNullOrWhiteSpace(target.Model))
        {
            scores.Add(source.Model.Equals(target.Model, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);
        }

        return scores.Count > 0 ? scores.Average() : 0.0;
    }

    private static bool PassesPreFilter(ListingData source, ListingData target)
    {
        // If year is known and differs by more than 1, likely not duplicate
        if (source.Year.HasValue && target.Year.HasValue &&
            Math.Abs(source.Year.Value - target.Year.Value) > 1)
        {
            return false;
        }

        // If make is known and different, skip
        if (!string.IsNullOrWhiteSpace(source.Make) && !string.IsNullOrWhiteSpace(target.Make) &&
            !source.Make.Equals(target.Make, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string NormalizeVin(string vin)
    {
        return vin.ToUpperInvariant()
            .Replace("O", "0")
            .Replace("I", "1")
            .Replace("Q", "0")
            .Replace(" ", "")
            .Replace("-", "");
    }
}
