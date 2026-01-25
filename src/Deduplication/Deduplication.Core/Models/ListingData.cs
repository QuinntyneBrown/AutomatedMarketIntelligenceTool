namespace Deduplication.Core.Models;

/// <summary>
/// Listing data used for deduplication matching.
/// </summary>
public sealed record ListingData
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? VIN { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public decimal? Price { get; init; }
    public int? Mileage { get; init; }
    public string? ImageHash { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string Source { get; init; } = string.Empty;
    public string SourceListingId { get; init; } = string.Empty;
}
