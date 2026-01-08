using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface ISearchService
{
    Task<SearchResult> SearchListingsAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default);
}

public class SearchCriteria
{
    public required Guid TenantId { get; init; }
    public string[]? Makes { get; init; }
    public string[]? Models { get; init; }
    public int? YearMin { get; init; }
    public int? YearMax { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public int? MileageMin { get; init; }
    public int? MileageMax { get; init; }
    public string? ZipCode { get; init; }
    public double? RadiusMiles { get; init; }
    public double? SearchLatitude { get; init; }
    public double? SearchLongitude { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
    public SearchSortField? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}

public class SearchResult
{
    public required List<ListingSearchResult> Listings { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ListingSearchResult
{
    public required Listing Listing { get; init; }
    public double? DistanceMiles { get; init; }
}

public enum SearchSortField
{
    Price,
    Year,
    Mileage,
    Distance,
    CreatedAt
}

public enum SortDirection
{
    Ascending,
    Descending
}
