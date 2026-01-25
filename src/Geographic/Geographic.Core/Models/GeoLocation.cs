namespace Geographic.Core.Models;

/// <summary>
/// Represents a geographic location with coordinates and address information.
/// </summary>
public sealed class GeoLocation
{
    /// <summary>
    /// The latitude coordinate.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude coordinate.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The city name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// The province or state name.
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// The postal or zip code.
    /// </summary>
    public string? PostalCode { get; set; }
}
