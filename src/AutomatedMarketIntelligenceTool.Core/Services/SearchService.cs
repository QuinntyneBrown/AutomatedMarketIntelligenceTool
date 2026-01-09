using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class SearchService : ISearchService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<SearchService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SearchResult> SearchListingsAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        if (criteria == null)
        {
            throw new ArgumentNullException(nameof(criteria));
        }

        _logger.LogInformation(
            "Searching listings for TenantId: {TenantId}, Makes: {Makes}, Models: {Models}, PriceRange: {PriceMin}-{PriceMax}, Page: {Page}",
            criteria.TenantId,
            criteria.Makes != null ? string.Join(",", criteria.Makes) : "ALL",
            criteria.Models != null ? string.Join(",", criteria.Models) : "ALL",
            criteria.PriceMin,
            criteria.PriceMax,
            criteria.Page);

        var query = _context.Listings.AsQueryable();

        // Apply tenant filter
        query = query.Where(l => l.TenantId == criteria.TenantId);

        // Apply make filter (case-insensitive)
        if (criteria.Makes?.Length > 0)
        {
            var makesUpper = criteria.Makes.Select(m => m.ToUpper()).ToArray();
            query = query.Where(l => makesUpper.Contains(l.Make.ToUpper()));
        }

        // Apply model filter (case-insensitive)
        if (criteria.Models?.Length > 0)
        {
            var modelsUpper = criteria.Models.Select(m => m.ToUpper()).ToArray();
            query = query.Where(l => modelsUpper.Contains(l.Model.ToUpper()));
        }

        // Apply year range filter
        if (criteria.YearMin.HasValue)
        {
            query = query.Where(l => l.Year >= criteria.YearMin.Value);
        }

        if (criteria.YearMax.HasValue)
        {
            query = query.Where(l => l.Year <= criteria.YearMax.Value);
        }

        // Apply price range filter
        if (criteria.PriceMin.HasValue)
        {
            query = query.Where(l => l.Price >= criteria.PriceMin.Value);
        }

        if (criteria.PriceMax.HasValue)
        {
            query = query.Where(l => l.Price <= criteria.PriceMax.Value);
        }

        // Apply mileage range filter
        if (criteria.MileageMin.HasValue)
        {
            query = query.Where(l => l.Mileage.HasValue && l.Mileage.Value >= criteria.MileageMin.Value);
        }

        if (criteria.MileageMax.HasValue)
        {
            query = query.Where(l => l.Mileage.HasValue && l.Mileage.Value <= criteria.MileageMax.Value);
        }

        // Apply condition filter
        if (criteria.Conditions?.Length > 0)
        {
            query = query.Where(l => criteria.Conditions.Contains(l.Condition));
        }

        // Apply transmission filter
        if (criteria.Transmissions?.Length > 0)
        {
            query = query.Where(l => l.Transmission.HasValue && criteria.Transmissions.Contains(l.Transmission.Value));
        }

        // Apply fuel type filter
        if (criteria.FuelTypes?.Length > 0)
        {
            query = query.Where(l => l.FuelType.HasValue && criteria.FuelTypes.Contains(l.FuelType.Value));
        }

        // Apply body style filter
        if (criteria.BodyStyles?.Length > 0)
        {
            query = query.Where(l => l.BodyStyle.HasValue && criteria.BodyStyles.Contains(l.BodyStyle.Value));
        }

        // Apply city filter (case-insensitive)
        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            query = query.Where(l => l.City != null && EF.Functions.Like(l.City, $"%{criteria.City}%"));
        }

        // Apply province filter (case-insensitive)
        if (!string.IsNullOrWhiteSpace(criteria.Province))
        {
            query = query.Where(l => l.Province != null && EF.Functions.Like(l.Province, criteria.Province));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        _logger.LogDebug("Found {TotalCount} listings matching criteria", totalCount);

        // Apply location filtering if coordinates are provided
        List<ListingSearchResult> results;
        if (criteria.SearchLatitude.HasValue && criteria.SearchLongitude.HasValue)
        {
            results = await ApplyLocationFilterAsync(
                query,
                criteria,
                cancellationToken);
        }
        else
        {
            // Apply sorting without location
            query = ApplySorting(query, criteria.SortBy, criteria.SortDirection);

            // Apply pagination
            query = query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize);

            var listings = await query.ToListAsync(cancellationToken);
            results = listings.Select(l => new ListingSearchResult
            {
                Listing = l,
                DistanceKilometers = null
            }).ToList();
        }

        // Populate price change information for all results
        await PopulatePriceChangesAsync(results, cancellationToken);

        // Calculate summary statistics
        var newListingsCount = results.Count(r => r.Listing.IsNewListing);
        var priceChangesCount = results.Count(r => r.PriceChange != null);

        _logger.LogInformation(
            "Search completed. Returned {ResultCount} listings out of {TotalCount} total ({NewCount} new, {PriceChangeCount} with price changes)",
            results.Count,
            totalCount,
            newListingsCount,
            priceChangesCount);

        return new SearchResult
        {
            Listings = results,
            TotalCount = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            NewListingsCount = newListingsCount,
            PriceChangesCount = priceChangesCount
        };
    }

    private async Task<List<ListingSearchResult>> ApplyLocationFilterAsync(
        IQueryable<Listing> query,
        SearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        // Fetch all listings (already filtered by other criteria)
        var listings = await query.ToListAsync(cancellationToken);

        // Calculate distances and filter by radius
        var results = listings
            .Select(l => new ListingSearchResult
            {
                Listing = l,
                DistanceKilometers = CalculateDistance(
                    criteria.SearchLatitude!.Value,
                    criteria.SearchLongitude!.Value,
                    l)
            })
            .Where(r => !criteria.RadiusKilometers.HasValue || 
                       (r.DistanceKilometers.HasValue && r.DistanceKilometers.Value <= criteria.RadiusKilometers.Value))
            .ToList();

        // Apply sorting
        results = ApplySortingWithDistance(results, criteria.SortBy, criteria.SortDirection);

        // Apply pagination
        results = results
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return results;
    }

    private static double? CalculateDistance(
        double searchLat,
        double searchLon,
        Listing listing)
    {
        // If listing doesn't have location data, return null
        if (string.IsNullOrWhiteSpace(listing.PostalCode))
        {
            return null;
        }

        // NOTE: This is a placeholder implementation.
        // In a production system, you would:
        // 1. Use a geocoding service to convert ZIP code to lat/lon
        // 2. Call CalculateHaversineDistance with the geocoded coordinates
        // For now, we return null to indicate distance cannot be calculated
        // without geocoding service integration
        return null;
    }

    // This method is ready for use once geocoding is integrated
    private static double CalculateHaversineDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadiusKilometers = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKilometers * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static IQueryable<Listing> ApplySorting(
        IQueryable<Listing> query,
        SearchSortField? sortBy,
        SortDirection direction)
    {
        if (!sortBy.HasValue)
        {
            return query.OrderByDescending(l => l.CreatedAt);
        }

        return sortBy.Value switch
        {
            SearchSortField.Price => direction == SortDirection.Ascending
                ? query.OrderBy(l => l.Price)
                : query.OrderByDescending(l => l.Price),
            SearchSortField.Year => direction == SortDirection.Ascending
                ? query.OrderBy(l => l.Year)
                : query.OrderByDescending(l => l.Year),
            SearchSortField.Mileage => direction == SortDirection.Ascending
                ? query.OrderBy(l => l.Mileage)
                : query.OrderByDescending(l => l.Mileage),
            SearchSortField.CreatedAt => direction == SortDirection.Ascending
                ? query.OrderBy(l => l.CreatedAt)
                : query.OrderByDescending(l => l.CreatedAt),
            SearchSortField.Distance => query.OrderByDescending(l => l.CreatedAt), // Distance sorting happens in-memory
            _ => query.OrderByDescending(l => l.CreatedAt)
        };
    }

    private static List<ListingSearchResult> ApplySortingWithDistance(
        List<ListingSearchResult> results,
        SearchSortField? sortBy,
        SortDirection direction)
    {
        if (!sortBy.HasValue || sortBy.Value != SearchSortField.Distance)
        {
            // Apply non-distance sorting
            if (!sortBy.HasValue)
            {
                return results.OrderByDescending(r => r.Listing.CreatedAt).ToList();
            }

            return sortBy.Value switch
            {
                SearchSortField.Price => direction == SortDirection.Ascending
                    ? results.OrderBy(r => r.Listing.Price).ToList()
                    : results.OrderByDescending(r => r.Listing.Price).ToList(),
                SearchSortField.Year => direction == SortDirection.Ascending
                    ? results.OrderBy(r => r.Listing.Year).ToList()
                    : results.OrderByDescending(r => r.Listing.Year).ToList(),
                SearchSortField.Mileage => direction == SortDirection.Ascending
                    ? results.OrderBy(r => r.Listing.Mileage).ToList()
                    : results.OrderByDescending(r => r.Listing.Mileage).ToList(),
                SearchSortField.CreatedAt => direction == SortDirection.Ascending
                    ? results.OrderBy(r => r.Listing.CreatedAt).ToList()
                    : results.OrderByDescending(r => r.Listing.CreatedAt).ToList(),
                _ => results.OrderByDescending(r => r.Listing.CreatedAt).ToList()
            };
        }

        // Sort by distance (handle nulls consistently)
        return direction == SortDirection.Ascending
            ? results.OrderBy(r => r.DistanceKilometers ?? double.MaxValue).ToList()
            : results.OrderByDescending(r => r.DistanceKilometers ?? double.MaxValue).ToList();
    }

    private async Task PopulatePriceChangesAsync(
        List<ListingSearchResult> results,
        CancellationToken cancellationToken)
    {
        if (results.Count == 0)
        {
            return;
        }

        // Get all listing IDs
        var listingIds = results.Select(r => r.Listing.ListingId).ToList();

        // Query all recent price histories for these listings
        var priceHistories = await _context.PriceHistory
            .Where(ph => listingIds.Contains(ph.ListingId))
            .Where(ph => ph.PriceChange.HasValue && ph.ChangePercentage.HasValue)
            .OrderByDescending(ph => ph.ObservedAt)
            .ToListAsync(cancellationToken);

        // Group in memory and get the most recent for each listing
        var priceHistoryLookup = priceHistories
            .GroupBy(ph => ph.ListingId)
            .ToDictionary(g => g.Key, g => g.First());

        // Populate price change information for each result
        foreach (var result in results)
        {
            if (priceHistoryLookup.TryGetValue(result.Listing.ListingId, out var priceHistory))
            {
                result.PriceChange = new ListingPriceChange
                {
                    PreviousPrice = priceHistory.Price - priceHistory.PriceChange!.Value,
                    CurrentPrice = priceHistory.Price,
                    PriceChange = priceHistory.PriceChange!.Value,
                    ChangePercentage = priceHistory.ChangePercentage!.Value,
                    ChangedAt = priceHistory.ObservedAt
                };
            }
        }
    }
}
