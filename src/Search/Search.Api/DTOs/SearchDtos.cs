namespace Search.Api.DTOs;

/// <summary>
/// Request for searching vehicles.
/// </summary>
public sealed record SearchRequest(
    string? Make,
    string? Model,
    int? YearFrom,
    int? YearTo,
    decimal? PriceFrom,
    decimal? PriceTo,
    int? MileageFrom,
    int? MileageTo,
    string? BodyStyle,
    string? Transmission,
    string? Drivetrain,
    string? FuelType,
    string? ExteriorColor,
    string? Province,
    string? City,
    int? RadiusKm,
    string? Keywords,
    bool? CertifiedPreOwned,
    bool? DealerOnly,
    bool? PrivateSellerOnly,
    int Skip = 0,
    int Take = 50);

/// <summary>
/// Response for a search operation.
/// </summary>
public sealed record SearchResponse(
    List<SearchResultItemResponse> Items,
    int TotalCount,
    int Skip,
    int Take,
    double SearchDurationMs);

/// <summary>
/// An individual search result item.
/// </summary>
public sealed record SearchResultItemResponse(
    Guid VehicleId,
    Guid? ListingId,
    string Make,
    string Model,
    int Year,
    string? Trim,
    decimal? Price,
    int? Mileage,
    string? BodyStyle,
    string? Transmission,
    string? Drivetrain,
    string? FuelType,
    string? ExteriorColor,
    string? Location,
    string? DealerName,
    string? ImageUrl,
    double? Score);

/// <summary>
/// Request for autocomplete suggestions.
/// </summary>
public sealed record AutoCompleteRequestDto(
    string Field,
    string Query,
    int MaxSuggestions = 10,
    SearchCriteriaDto? Context = null);

/// <summary>
/// Response for autocomplete suggestions.
/// </summary>
public sealed record AutoCompleteSuggestionResponse(
    string Value,
    string DisplayText,
    int Count);

/// <summary>
/// Search criteria DTO for context in autocomplete.
/// </summary>
public sealed record SearchCriteriaDto(
    string? Make,
    string? Model,
    int? YearFrom,
    int? YearTo,
    decimal? PriceFrom,
    decimal? PriceTo,
    int? MileageFrom,
    int? MileageTo,
    string? BodyStyle,
    string? Transmission,
    string? Drivetrain,
    string? FuelType,
    string? ExteriorColor,
    string? Province,
    string? City,
    int? RadiusKm,
    string? Keywords,
    bool? CertifiedPreOwned,
    bool? DealerOnly,
    bool? PrivateSellerOnly);

/// <summary>
/// Response for search facets.
/// </summary>
public sealed record SearchFacetsResponse(
    List<FacetValueResponse> Makes,
    List<FacetValueResponse> Models,
    List<FacetValueResponse> Years,
    List<FacetValueResponse> BodyStyles,
    List<FacetValueResponse> Transmissions,
    List<FacetValueResponse> Drivetrains,
    List<FacetValueResponse> FuelTypes,
    List<FacetValueResponse> Colors,
    PriceRangeResponse PriceRange,
    MileageRangeResponse MileageRange);

/// <summary>
/// A facet value with count.
/// </summary>
public sealed record FacetValueResponse(string Value, int Count);

/// <summary>
/// Price range for faceted search.
/// </summary>
public sealed record PriceRangeResponse(decimal Min, decimal Max);

/// <summary>
/// Mileage range for faceted search.
/// </summary>
public sealed record MileageRangeResponse(int Min, int Max);

/// <summary>
/// Request for creating a search profile.
/// </summary>
public sealed record CreateSearchProfileRequest(
    string Name,
    SearchCriteriaDto Criteria,
    Guid? UserId = null);

/// <summary>
/// Request for updating a search profile.
/// </summary>
public sealed record UpdateSearchProfileRequest(
    string? Name,
    SearchCriteriaDto? Criteria);

/// <summary>
/// Response for a search profile.
/// </summary>
public sealed record SearchProfileResponse(
    Guid Id,
    string Name,
    SearchCriteriaDto Criteria,
    bool IsActive,
    Guid? UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
