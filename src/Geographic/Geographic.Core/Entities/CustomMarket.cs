namespace Geographic.Core.Entities;

/// <summary>
/// Represents a custom geographic market area defined by a center point and radius.
/// </summary>
public sealed class CustomMarket
{
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the custom market.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude of the market center point.
    /// </summary>
    public double CenterLatitude { get; set; }

    /// <summary>
    /// The longitude of the market center point.
    /// </summary>
    public double CenterLongitude { get; set; }

    /// <summary>
    /// The radius of the market area in kilometers.
    /// </summary>
    public double RadiusKm { get; set; }

    /// <summary>
    /// Comma-separated list of postal codes within this market.
    /// </summary>
    public string? PostalCodes { get; set; }

    /// <summary>
    /// When this market was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
