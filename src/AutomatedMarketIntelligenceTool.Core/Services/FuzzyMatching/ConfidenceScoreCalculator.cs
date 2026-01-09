namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculates composite confidence scores for fuzzy matching.
/// </summary>
public class ConfidenceScoreCalculator
{
    private readonly ILevenshteinCalculator _levenshtein;
    private readonly INumericProximityCalculator _numeric;
    private readonly IGeoDistanceCalculator _geo;

    private static readonly Dictionary<string, decimal> FieldWeights = new()
    {
        ["Make"] = 0.15m,
        ["Model"] = 0.20m,
        ["Year"] = 0.15m,
        ["Mileage"] = 0.15m,
        ["Price"] = 0.20m,
        ["Location"] = 0.15m
    };

    public ConfidenceScoreCalculator(
        ILevenshteinCalculator levenshtein,
        INumericProximityCalculator numeric,
        IGeoDistanceCalculator geo)
    {
        _levenshtein = levenshtein ?? throw new ArgumentNullException(nameof(levenshtein));
        _numeric = numeric ?? throw new ArgumentNullException(nameof(numeric));
        _geo = geo ?? throw new ArgumentNullException(nameof(geo));
    }

    /// <summary>
    /// Calculates fuzzy match confidence score for a listing pair.
    /// </summary>
    /// <param name="existing">Existing listing.</param>
    /// <param name="incoming">Incoming scraped listing.</param>
    /// <returns>Fuzzy match result with confidence score and field scores.</returns>
    public FuzzyMatchResult Calculate(
        Models.ListingAggregate.Listing existing,
        ScrapedListingData incoming)
    {
        var scores = new Dictionary<string, decimal>
        {
            ["Make"] = _levenshtein.Calculate(existing.Make, incoming.Make),
            ["Model"] = _levenshtein.Calculate(existing.Model, incoming.Model),
            ["Year"] = _numeric.CalculateYearSimilarity(existing.Year, incoming.Year, toleranceYears: 1),
            ["Mileage"] = CalculateMileageSimilarity(existing.Mileage, incoming.Mileage),
            ["Price"] = _numeric.Calculate(existing.Price, incoming.Price, tolerance: 500m),
            ["Location"] = _geo.Calculate(
                existing.Latitude,
                existing.Longitude,
                incoming.Latitude,
                incoming.Longitude,
                toleranceMiles: 10m)
        };

        var compositeScore = scores
            .Sum(kvp => kvp.Value * FieldWeights[kvp.Key]) * 100m;

        return new FuzzyMatchResult
        {
            MatchedListing = existing,
            ConfidenceScore = Math.Round(compositeScore, 2),
            FieldScores = scores,
            Method = MatchMethod.FuzzyAttributes
        };
    }

    private decimal CalculateMileageSimilarity(int? mileage1, int? mileage2)
    {
        // If both are null, consider them similar
        if (!mileage1.HasValue && !mileage2.HasValue)
        {
            return 0.5m;
        }

        // If only one is null, low similarity
        if (!mileage1.HasValue || !mileage2.HasValue)
        {
            return 0.2m;
        }

        return _numeric.Calculate(mileage1.Value, mileage2.Value, tolerance: 500m);
    }
}
