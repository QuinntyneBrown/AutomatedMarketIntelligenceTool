using Deduplication.Core.Interfaces.Calculators;

namespace Deduplication.Infrastructure.Calculators;

public sealed class LocationProximityCalculator : ILocationProximityCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    public double CalculateLocationSimilarity(
        double? lat1, double? lon1,
        double? lat2, double? lon2,
        double maxDistanceKm = 50)
    {
        if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
            return 0.5; // Neutral score when location unknown

        var distance = CalculateDistanceKm(lat1.Value, lon1.Value, lat2.Value, lon2.Value);

        if (distance >= maxDistanceKm)
            return 0.0;

        return 1.0 - distance / maxDistanceKm;
    }

    public double CalculatePostalCodeSimilarity(string? postalCode1, string? postalCode2)
    {
        if (string.IsNullOrWhiteSpace(postalCode1) && string.IsNullOrWhiteSpace(postalCode2))
            return 0.5; // Neutral score

        if (string.IsNullOrWhiteSpace(postalCode1) || string.IsNullOrWhiteSpace(postalCode2))
            return 0.3;

        var normalized1 = NormalizePostalCode(postalCode1);
        var normalized2 = NormalizePostalCode(postalCode2);

        // Exact match
        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // Canadian postal code - match FSA (first 3 chars)
        if (normalized1.Length >= 3 && normalized2.Length >= 3)
        {
            var fsa1 = normalized1[..3];
            var fsa2 = normalized2[..3];
            if (fsa1.Equals(fsa2, StringComparison.OrdinalIgnoreCase))
                return 0.8;
        }

        // US ZIP - match first 3 digits
        if (normalized1.Length >= 3 && normalized2.Length >= 3 &&
            char.IsDigit(normalized1[0]) && char.IsDigit(normalized2[0]))
        {
            if (normalized1[..3] == normalized2[..3])
                return 0.7;
        }

        return 0.2;
    }

    public double CalculateCombinedLocationSimilarity(
        double? lat1, double? lon1, string? city1, string? province1, string? postal1,
        double? lat2, double? lon2, string? city2, string? province2, string? postal2)
    {
        var scores = new List<(double score, double weight)>();

        // Coordinates are most accurate
        if (lat1.HasValue && lon1.HasValue && lat2.HasValue && lon2.HasValue)
        {
            scores.Add((CalculateLocationSimilarity(lat1, lon1, lat2, lon2), 3.0));
        }

        // Postal code match
        if (!string.IsNullOrWhiteSpace(postal1) || !string.IsNullOrWhiteSpace(postal2))
        {
            scores.Add((CalculatePostalCodeSimilarity(postal1, postal2), 2.0));
        }

        // City match
        if (!string.IsNullOrWhiteSpace(city1) && !string.IsNullOrWhiteSpace(city2))
        {
            var cityScore = city1.Equals(city2, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            scores.Add((cityScore, 1.0));
        }

        // Province/state match
        if (!string.IsNullOrWhiteSpace(province1) && !string.IsNullOrWhiteSpace(province2))
        {
            var provinceScore = province1.Equals(province2, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            scores.Add((provinceScore, 0.5));
        }

        if (scores.Count == 0)
            return 0.5; // Neutral when no location data

        var totalWeight = scores.Sum(s => s.weight);
        var weightedSum = scores.Sum(s => s.score * s.weight);

        return weightedSum / totalWeight;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static string NormalizePostalCode(string postalCode) =>
        postalCode.Replace(" ", "").Replace("-", "").ToUpperInvariant();
}
