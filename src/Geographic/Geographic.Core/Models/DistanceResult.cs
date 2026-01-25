namespace Geographic.Core.Models;

/// <summary>
/// Represents the result of a distance calculation between two locations.
/// </summary>
public sealed class DistanceResult
{
    /// <summary>
    /// The starting location.
    /// </summary>
    public GeoLocation FromLocation { get; set; } = null!;

    /// <summary>
    /// The destination location.
    /// </summary>
    public GeoLocation ToLocation { get; set; } = null!;

    /// <summary>
    /// The distance between the two locations in kilometers.
    /// </summary>
    public double DistanceKm { get; set; }
}
