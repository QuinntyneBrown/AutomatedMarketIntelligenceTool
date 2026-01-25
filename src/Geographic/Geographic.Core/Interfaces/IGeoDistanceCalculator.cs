using Geographic.Core.Models;

namespace Geographic.Core.Interfaces;

/// <summary>
/// Service interface for geographic distance calculations and geocoding.
/// </summary>
public interface IGeoDistanceCalculator
{
    /// <summary>
    /// Calculates the distance between two geographic points using the Haversine formula.
    /// </summary>
    /// <param name="fromLatitude">Starting latitude.</param>
    /// <param name="fromLongitude">Starting longitude.</param>
    /// <param name="toLatitude">Destination latitude.</param>
    /// <param name="toLongitude">Destination longitude.</param>
    /// <returns>Distance in kilometers.</returns>
    double CalculateDistance(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude);

    /// <summary>
    /// Calculates the distance between two GeoLocation objects.
    /// </summary>
    DistanceResult CalculateDistance(GeoLocation from, GeoLocation to);

    /// <summary>
    /// Determines if a point is within a specified radius of a center point.
    /// </summary>
    /// <param name="centerLatitude">Center latitude.</param>
    /// <param name="centerLongitude">Center longitude.</param>
    /// <param name="pointLatitude">Point latitude to check.</param>
    /// <param name="pointLongitude">Point longitude to check.</param>
    /// <param name="radiusKm">Radius in kilometers.</param>
    /// <returns>True if the point is within the radius.</returns>
    bool IsWithinRadius(double centerLatitude, double centerLongitude, double pointLatitude, double pointLongitude, double radiusKm);

    /// <summary>
    /// Geocodes an address or postal code to coordinates.
    /// This is a simplified implementation that returns mock data.
    /// In production, this would integrate with a geocoding service.
    /// </summary>
    /// <param name="address">The address or postal code to geocode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The geocoded location, or null if not found.</returns>
    Task<GeoLocation?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
