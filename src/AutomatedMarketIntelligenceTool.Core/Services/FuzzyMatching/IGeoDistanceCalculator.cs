namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculator for geographic distance similarity.
/// </summary>
public interface IGeoDistanceCalculator : ISimilarityCalculator
{
    /// <summary>
    /// Calculates similarity based on geographic distance between two points.
    /// </summary>
    /// <param name="lat1">Latitude of first point.</param>
    /// <param name="lon1">Longitude of first point.</param>
    /// <param name="lat2">Latitude of second point.</param>
    /// <param name="lon2">Longitude of second point.</param>
    /// <param name="toleranceMiles">Maximum distance in miles for perfect match.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    decimal Calculate(decimal? lat1, decimal? lon1, decimal? lat2, decimal? lon2, decimal toleranceMiles);
}
