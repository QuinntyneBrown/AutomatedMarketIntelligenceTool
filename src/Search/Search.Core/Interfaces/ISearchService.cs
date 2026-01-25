using Search.Core.Entities;

namespace Search.Core.Interfaces;

/// <summary>
/// Interface for the search service.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for vehicles based on the given criteria.
    /// </summary>
    Task<SearchResult> SearchAsync(SearchCriteria criteria, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets autocomplete suggestions for a given field and query.
    /// </summary>
    Task<IReadOnlyList<AutoCompleteSuggestion>> GetAutoCompleteAsync(AutoCompleteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets faceted search options based on the current criteria.
    /// </summary>
    Task<SearchFacets> GetFacetsAsync(SearchCriteria criteria, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the search profile service.
/// </summary>
public interface ISearchProfileService
{
    Task<SearchProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchProfile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default);
    Task<SearchProfile> CreateAsync(string name, SearchCriteria criteria, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<SearchProfile> UpdateAsync(Guid id, string? name = null, SearchCriteria? criteria = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a search operation.
/// </summary>
public sealed record SearchResult
{
    public IReadOnlyList<SearchResultItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
    public TimeSpan SearchDuration { get; init; }
}

/// <summary>
/// An individual search result item.
/// </summary>
public sealed record SearchResultItem
{
    public Guid VehicleId { get; init; }
    public Guid? ListingId { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Trim { get; init; }
    public decimal? Price { get; init; }
    public int? Mileage { get; init; }
    public string? BodyStyle { get; init; }
    public string? Transmission { get; init; }
    public string? Drivetrain { get; init; }
    public string? FuelType { get; init; }
    public string? ExteriorColor { get; init; }
    public string? Location { get; init; }
    public string? DealerName { get; init; }
    public string? ImageUrl { get; init; }
    public double? Score { get; init; }
}

/// <summary>
/// Request for autocomplete suggestions.
/// </summary>
public sealed record AutoCompleteRequest
{
    public string Field { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public int MaxSuggestions { get; init; } = 10;
    public SearchCriteria? Context { get; init; }
}

/// <summary>
/// An autocomplete suggestion.
/// </summary>
public sealed record AutoCompleteSuggestion
{
    public string Value { get; init; } = string.Empty;
    public string DisplayText { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>
/// Faceted search options.
/// </summary>
public sealed record SearchFacets
{
    public IReadOnlyList<FacetValue> Makes { get; init; } = [];
    public IReadOnlyList<FacetValue> Models { get; init; } = [];
    public IReadOnlyList<FacetValue> Years { get; init; } = [];
    public IReadOnlyList<FacetValue> BodyStyles { get; init; } = [];
    public IReadOnlyList<FacetValue> Transmissions { get; init; } = [];
    public IReadOnlyList<FacetValue> Drivetrains { get; init; } = [];
    public IReadOnlyList<FacetValue> FuelTypes { get; init; } = [];
    public IReadOnlyList<FacetValue> Colors { get; init; } = [];
    public PriceRange PriceRange { get; init; } = new();
    public MileageRange MileageRange { get; init; } = new();
}

/// <summary>
/// A facet value with count.
/// </summary>
public sealed record FacetValue
{
    public string Value { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>
/// Price range for faceted search.
/// </summary>
public sealed record PriceRange
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
}

/// <summary>
/// Mileage range for faceted search.
/// </summary>
public sealed record MileageRange
{
    public int Min { get; init; }
    public int Max { get; init; }
}
