using ScrapingOrchestration.Core.Enums;

namespace ScrapingWorker.Core.Models;

/// <summary>
/// Represents a listing scraped from a source.
/// </summary>
public sealed record ScrapedListing
{
    public required string SourceListingId { get; init; }
    public required ScrapingSource Source { get; init; }
    public required string Title { get; init; }
    public decimal? Price { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int? Mileage { get; init; }
    public string? VIN { get; init; }
    public string? Condition { get; init; }
    public string? BodyStyle { get; init; }
    public string? Transmission { get; init; }
    public string? FuelType { get; init; }
    public string? Drivetrain { get; init; }
    public string? ExteriorColor { get; init; }
    public string? InteriorColor { get; init; }
    public string? Engine { get; init; }
    public string? DealerName { get; init; }
    public string? DealerPhone { get; init; }
    public string? DealerAddress { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string? ListingUrl { get; init; }
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
    public string? Description { get; init; }
    public DateTimeOffset ScrapedAt { get; init; }
    public IDictionary<string, string> AdditionalData { get; init; } = new Dictionary<string, string>();
}
