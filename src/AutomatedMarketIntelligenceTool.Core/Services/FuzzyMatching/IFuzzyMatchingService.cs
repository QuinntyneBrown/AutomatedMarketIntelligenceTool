using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Service for fuzzy matching of vehicle listings.
/// </summary>
public interface IFuzzyMatchingService
{
    /// <summary>
    /// Finds the best matching listing for a scraped listing.
    /// </summary>
    /// <param name="listing">Scraped listing data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fuzzy match result with confidence score.</returns>
    Task<FuzzyMatchResult> FindBestMatchAsync(
        ScrapedListingData listing,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Data for a scraped listing used in fuzzy matching.
/// </summary>
public class ScrapedListingData
{
    public required Guid TenantId { get; init; }
    public required string ExternalId { get; init; }
    public required string SourceSite { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int Year { get; init; }
    public required decimal Price { get; init; }
    public int? Mileage { get; init; }
    public string? Vin { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

/// <summary>
/// Result of a fuzzy match operation.
/// </summary>
public class FuzzyMatchResult
{
    public Listing? MatchedListing { get; init; }
    public decimal ConfidenceScore { get; init; }
    public MatchMethod Method { get; init; }
    public Dictionary<string, decimal> FieldScores { get; init; } = new();
}

/// <summary>
/// Method used to match listings.
/// </summary>
public enum MatchMethod
{
    None,
    ExactVin,
    PartialVin,
    ExternalId,
    FuzzyAttributes
}
