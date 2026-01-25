namespace VehicleAggregation.Api.DTOs;

public sealed record VehicleResponse(
    Guid Id,
    string? VIN,
    string Make,
    string Model,
    int Year,
    string? Trim,
    string? BodyStyle,
    string? Transmission,
    string? Drivetrain,
    string? FuelType,
    decimal? BestPrice,
    decimal? AveragePrice,
    decimal? LowestPrice,
    decimal? HighestPrice,
    int ListingCount,
    Guid? BestPriceListingId,
    List<VehicleListingResponse> Listings);

public sealed record VehicleListingResponse(
    Guid Id,
    Guid ListingId,
    decimal Price,
    string Source,
    string? DealerName,
    DateTimeOffset AddedAt);

public sealed record CreateVehicleRequest(
    string? VIN,
    string Make,
    string Model,
    int Year,
    string? Trim,
    string? BodyStyle,
    string? Transmission,
    string? Drivetrain,
    string? FuelType,
    string? ExteriorColor,
    string? InteriorColor);

public sealed record LinkListingRequest(
    Guid ListingId,
    decimal Price,
    string Source,
    string? DealerName);

public sealed record VehicleSearchRequest(
    string? Make,
    string? Model,
    int? YearFrom,
    int? YearTo,
    decimal? PriceFrom,
    decimal? PriceTo,
    int Skip = 0,
    int Take = 50);
