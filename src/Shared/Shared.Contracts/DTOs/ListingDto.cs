using Shared.Contracts.Enums;

namespace Shared.Contracts.DTOs;

/// <summary>
/// Data transfer object for listing information across services.
/// </summary>
public record ListingDto
{
    public Guid ListingId { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string SourceSite { get; init; } = string.Empty;
    public string ListingUrl { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Trim { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "CAD";
    public int? Mileage { get; init; }
    public string? Vin { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public Condition Condition { get; init; }
    public Transmission? Transmission { get; init; }
    public FuelType? FuelType { get; init; }
    public BodyStyle? BodyStyle { get; init; }
    public Drivetrain? Drivetrain { get; init; }
    public string? ExteriorColor { get; init; }
    public string? InteriorColor { get; init; }
    public SellerType? SellerType { get; init; }
    public string? SellerName { get; init; }
    public string? Description { get; init; }
    public List<string> ImageUrls { get; init; } = new();
    public DateTime? ListingDate { get; init; }
    public DateTime FirstSeenDate { get; init; }
    public DateTime LastSeenDate { get; init; }
    public bool IsActive { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}
