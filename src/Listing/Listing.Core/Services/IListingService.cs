namespace Listing.Core.Services;

/// <summary>
/// Service interface for listing operations.
/// </summary>
public interface IListingService
{
    /// <summary>
    /// Gets a listing by ID.
    /// </summary>
    Task<Entities.Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a listing by source and source listing ID.
    /// </summary>
    Task<Entities.Listing?> GetBySourceIdAsync(string source, string sourceListingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new listing.
    /// </summary>
    Task<Entities.Listing> CreateAsync(Entities.Listing listing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing listing.
    /// </summary>
    Task UpdateAsync(Entities.Listing listing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a listing.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches listings with filters.
    /// </summary>
    Task<IReadOnlyList<Entities.Listing>> SearchAsync(
        string? make = null,
        string? model = null,
        int? yearFrom = null,
        int? yearTo = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? maxMileage = null,
        string? province = null,
        bool? isActive = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history for a listing.
    /// </summary>
    Task<IReadOnlyList<Entities.PriceHistory>> GetPriceHistoryAsync(Guid listingId, CancellationToken cancellationToken = default);
}
