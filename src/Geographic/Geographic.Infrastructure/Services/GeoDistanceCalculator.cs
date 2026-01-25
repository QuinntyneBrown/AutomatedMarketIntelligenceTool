using Geographic.Core.Interfaces;
using Geographic.Core.Models;

namespace Geographic.Infrastructure.Services;

/// <summary>
/// Service implementation for geographic distance calculations using the Haversine formula.
/// </summary>
public sealed class GeoDistanceCalculator : IGeoDistanceCalculator
{
    /// <summary>
    /// Earth's radius in kilometers.
    /// </summary>
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculates the distance between two geographic points using the Haversine formula.
    /// The Haversine formula determines the great-circle distance between two points on a sphere
    /// given their longitudes and latitudes.
    /// </summary>
    public double CalculateDistance(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude)
    {
        var dLat = DegreesToRadians(toLatitude - fromLatitude);
        var dLon = DegreesToRadians(toLongitude - fromLongitude);

        var lat1Rad = DegreesToRadians(fromLatitude);
        var lat2Rad = DegreesToRadians(toLatitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Calculates the distance between two GeoLocation objects.
    /// </summary>
    public DistanceResult CalculateDistance(GeoLocation from, GeoLocation to)
    {
        var distanceKm = CalculateDistance(from.Latitude, from.Longitude, to.Latitude, to.Longitude);

        return new DistanceResult
        {
            FromLocation = from,
            ToLocation = to,
            DistanceKm = distanceKm
        };
    }

    /// <summary>
    /// Determines if a point is within a specified radius of a center point.
    /// </summary>
    public bool IsWithinRadius(double centerLatitude, double centerLongitude, double pointLatitude, double pointLongitude, double radiusKm)
    {
        var distance = CalculateDistance(centerLatitude, centerLongitude, pointLatitude, pointLongitude);
        return distance <= radiusKm;
    }

    /// <summary>
    /// Geocodes an address or postal code to coordinates.
    /// This is a simplified mock implementation that returns sample data for demonstration.
    /// In production, this would integrate with a geocoding service like Google Maps, Azure Maps, etc.
    /// </summary>
    public Task<GeoLocation?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        // Mock geocoding implementation for demonstration
        // In production, this would call an external geocoding API
        var mockLocations = new Dictionary<string, GeoLocation>(StringComparer.OrdinalIgnoreCase)
        {
            ["Toronto"] = new GeoLocation { Latitude = 43.6532, Longitude = -79.3832, City = "Toronto", Province = "Ontario", PostalCode = "M5H 2N2" },
            ["Vancouver"] = new GeoLocation { Latitude = 49.2827, Longitude = -123.1207, City = "Vancouver", Province = "British Columbia", PostalCode = "V6C 3L2" },
            ["Montreal"] = new GeoLocation { Latitude = 45.5017, Longitude = -73.5673, City = "Montreal", Province = "Quebec", PostalCode = "H3B 2Y5" },
            ["Calgary"] = new GeoLocation { Latitude = 51.0447, Longitude = -114.0719, City = "Calgary", Province = "Alberta", PostalCode = "T2P 1J9" },
            ["Ottawa"] = new GeoLocation { Latitude = 45.4215, Longitude = -75.6972, City = "Ottawa", Province = "Ontario", PostalCode = "K1P 1J1" },
            ["Edmonton"] = new GeoLocation { Latitude = 53.5461, Longitude = -113.4938, City = "Edmonton", Province = "Alberta", PostalCode = "T5J 0N3" },
            ["Mississauga"] = new GeoLocation { Latitude = 43.5890, Longitude = -79.6441, City = "Mississauga", Province = "Ontario", PostalCode = "L5B 3C2" },
            ["Winnipeg"] = new GeoLocation { Latitude = 49.8951, Longitude = -97.1384, City = "Winnipeg", Province = "Manitoba", PostalCode = "R3C 4A4" },
            ["M5H 2N2"] = new GeoLocation { Latitude = 43.6532, Longitude = -79.3832, City = "Toronto", Province = "Ontario", PostalCode = "M5H 2N2" },
            ["V6C 3L2"] = new GeoLocation { Latitude = 49.2827, Longitude = -123.1207, City = "Vancouver", Province = "British Columbia", PostalCode = "V6C 3L2" }
        };

        // Try to find an exact match first
        if (mockLocations.TryGetValue(address, out var location))
        {
            return Task.FromResult<GeoLocation?>(location);
        }

        // Try to find a partial match
        var matchingKey = mockLocations.Keys.FirstOrDefault(k =>
            address.Contains(k, StringComparison.OrdinalIgnoreCase) ||
            k.Contains(address, StringComparison.OrdinalIgnoreCase));

        if (matchingKey != null)
        {
            return Task.FromResult<GeoLocation?>(mockLocations[matchingKey]);
        }

        return Task.FromResult<GeoLocation?>(null);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
