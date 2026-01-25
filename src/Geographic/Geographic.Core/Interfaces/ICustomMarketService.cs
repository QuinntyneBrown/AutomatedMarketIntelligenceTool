using Geographic.Core.Entities;

namespace Geographic.Core.Interfaces;

/// <summary>
/// Service interface for managing custom market areas.
/// </summary>
public interface ICustomMarketService
{
    /// <summary>
    /// Creates a new custom market.
    /// </summary>
    Task<CustomMarket> CreateAsync(CustomMarket market, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a custom market by its identifier.
    /// </summary>
    Task<CustomMarket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all custom markets.
    /// </summary>
    Task<IEnumerable<CustomMarket>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing custom market.
    /// </summary>
    Task<CustomMarket> UpdateAsync(CustomMarket market, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom market by its identifier.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for markets by name.
    /// </summary>
    Task<IEnumerable<CustomMarket>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
}
