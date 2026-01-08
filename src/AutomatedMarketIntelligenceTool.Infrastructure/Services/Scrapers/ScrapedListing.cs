using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class ScrapedListing
{
    public required string ExternalId { get; set; }
    public required string SourceSite { get; set; }
    public required string ListingUrl { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required int Year { get; set; }
    public string? Trim { get; set; }
    public required decimal Price { get; set; }
    public int? Mileage { get; set; }
    public string? Vin { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string Currency { get; set; } = "CAD";
    public string Country { get; set; } = "CA";
    public required Condition Condition { get; set; }
    public Transmission? Transmission { get; set; }
    public FuelType? FuelType { get; set; }
    public string? BodyStyle { get; set; }
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
