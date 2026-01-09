namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculates geographic distance similarity using Haversine formula.
/// </summary>
public class GeoDistanceCalculator : IGeoDistanceCalculator
{
    private const decimal EarthRadiusMiles = 3959m;

    /// <inheritdoc/>
    public decimal Calculate()
    {
        throw new NotImplementedException("Use Calculate(lat1, lon1, lat2, lon2, toleranceMiles) instead.");
    }

    /// <inheritdoc/>
    public decimal Calculate(decimal? lat1, decimal? lon1, decimal? lat2, decimal? lon2, decimal toleranceMiles)
    {
        // If any coordinate is missing, return 0 similarity
        if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
        {
            return 0.0m;
        }

        if (toleranceMiles <= 0)
        {
            throw new ArgumentException("Tolerance must be greater than zero.", nameof(toleranceMiles));
        }

        var distanceMiles = CalculateDistance(lat1.Value, lon1.Value, lat2.Value, lon2.Value);

        // Within tolerance - perfect match
        if (distanceMiles <= toleranceMiles)
        {
            return 1.0m;
        }

        // Calculate similarity based on distance beyond tolerance
        var similarity = toleranceMiles / distanceMiles;
        
        return Math.Min(1.0m, similarity);
    }

    /// <summary>
    /// Calculates distance between two geographic points using Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of first point in degrees.</param>
    /// <param name="lon1">Longitude of first point in degrees.</param>
    /// <param name="lat2">Latitude of second point in degrees.</param>
    /// <param name="lon2">Longitude of second point in degrees.</param>
    /// <returns>Distance in miles.</returns>
    private static decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        // Convert to radians
        var lat1Rad = DegreesToRadians(lat1);
        var lon1Rad = DegreesToRadians(lon1);
        var lat2Rad = DegreesToRadians(lat2);
        var lon2Rad = DegreesToRadians(lon2);

        // Haversine formula
        var dLat = lat2Rad - lat1Rad;
        var dLon = lon2Rad - lon1Rad;

        var a = (decimal)(
            Math.Pow(Math.Sin((double)(dLat / 2)), 2) +
            Math.Cos((double)lat1Rad) * Math.Cos((double)lat2Rad) *
            Math.Pow(Math.Sin((double)(dLon / 2)), 2));

        var c = 2 * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a)));

        return EarthRadiusMiles * c;
    }

    private static decimal DegreesToRadians(decimal degrees)
    {
        return degrees * (decimal)Math.PI / 180m;
    }
}
