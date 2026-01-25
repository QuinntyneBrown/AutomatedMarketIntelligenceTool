using Shared.Contracts.Enums;

namespace Listing.Api.DTOs;

/// <summary>
/// Request to create a listing.
/// </summary>
public sealed record CreateListingRequest
{
    public required string SourceListingId { get; init; }
    public required string Source { get; init; }
    public required string Title { get; init; }
    public decimal? Price { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int? Mileage { get; init; }
    public string? VIN { get; init; }
    public Condition? Condition { get; init; }
    public BodyStyle? BodyStyle { get; init; }
    public Transmission? Transmission { get; init; }
    public FuelType? FuelType { get; init; }
    public Drivetrain? Drivetrain { get; init; }
    public string? ExteriorColor { get; init; }
    public string? InteriorColor { get; init; }
    public string? DealerName { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string? ListingUrl { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string>? ImageUrls { get; init; }
}

/// <summary>
/// Request to update a listing.
/// </summary>
public sealed record UpdateListingRequest
{
    public string? Title { get; init; }
    public decimal? Price { get; init; }
    public int? Mileage { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string>? ImageUrls { get; init; }
}

/// <summary>
/// Response for a listing.
/// </summary>
public sealed record ListingResponse
{
    public Guid Id { get; init; }
    public string SourceListingId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
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
    public string? DealerName { get; init; }
    public Guid? DealerId { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string? ListingUrl { get; init; }
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Response for price history.
/// </summary>
public sealed record PriceHistoryResponse
{
    public decimal Price { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
}

/// <summary>
/// Request for searching listings.
/// </summary>
public sealed record SearchListingsRequest
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MaxMileage { get; init; }
    public string? Province { get; init; }
    public bool? IsActive { get; init; } = true;
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}
