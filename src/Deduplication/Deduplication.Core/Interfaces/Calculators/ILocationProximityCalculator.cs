namespace Deduplication.Core.Interfaces.Calculators;

/// <summary>
/// Calculator for geographic location proximity.
/// </summary>
public interface ILocationProximityCalculator
{
    /// <summary>
    /// Calculates the Haversine distance between two coordinates in kilometers.
    /// </summary>
    double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);

    /// <summary>
    /// Calculates similarity (0-1) based on distance.
    /// </summary>
    double CalculateLocationSimilarity(
        double? lat1, double? lon1,
        double? lat2, double? lon2,
        double maxDistanceKm = 50);

    /// <summary>
    /// Calculates similarity (0-1) based on postal code matching.
    /// </summary>
    double CalculatePostalCodeSimilarity(string? postalCode1, string? postalCode2);

    /// <summary>
    /// Calculates combined location similarity using available data.
    /// </summary>
    double CalculateCombinedLocationSimilarity(
        double? lat1, double? lon1, string? city1, string? province1, string? postal1,
        double? lat2, double? lon2, string? city2, string? province2, string? postal2);
}
